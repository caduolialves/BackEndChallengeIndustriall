using PurchaseOrderChallenge.Models;
using PurchaseOrderChallenge.Models.DTOs;
using PurchaseOrderChallenge.Models.Enums;
using PurchaseOrderChallenge.Repository.Interfaces;
using PurchaseOrderChallenge.Service.Interfaces;

namespace PurchaseOrderChallenge.Service;

public class PurchaseOrderService(
        IPurchaseRequestRepository repository,
        IApprovalStepsRepository approvalStepsRepository,
        IPurchaseRequestHistoryRepository purchaseRequestHistoryRepository)
    : IPurchaseOrderService
{
    private readonly IPurchaseRequestRepository _purchaseRequestRepository = repository;
    private readonly IApprovalStepsRepository _approvalStepsRepository = approvalStepsRepository;
    private readonly IPurchaseRequestHistoryRepository _purchaseRequestHistoryRepository = purchaseRequestHistoryRepository;

    /// <summary>
    /// Cria um novo pedido de compra.
    /// RN2: calcula o valor total.
    /// RN3: cria a cadeia de aprovação conforme a alçada.
    /// RN6: registra a criação no histórico.
    /// </summary>
    public void CreatePurchaseRequest(PurchaseRequest request)
    {
        // RN2: o total precisa ser calculado antes da definição da alçada.
        CalculateTotalAmount(request);

        request.CreatedAt = DateTime.UtcNow;
        request.UpdatedAt = request.CreatedAt;

        // RN3: cria as etapas de aprovação exigidas pelo valor total do pedido.
        request.ApprovalSteps = CreateApprovalSteps(request);

        // RN4: todo fluxo começa pela primeira etapa da sequência, que é Suprimentos.
        SetPendingStatusByUserRole(request, UserRole.Supply);

        var insertedRequest = _purchaseRequestRepository.Insert(request);

        // RN6: toda criação de pedido deve ser registrada no histórico.
        _purchaseRequestHistoryRepository.Insert(new PurchaseRequestHistory
        {
            PurchaseRequestId = insertedRequest.Id,
            ActionType = HistoryActionType.Created,
            PerformedBy = insertedRequest.RequesterName,
            PerformedByRole = UserRole.Requester,
            Comments = "Pedido criado."
        });
    }

    /// <summary>
    /// Retorna todos os pedidos de compra cadastrados.
    /// </summary>
    public IEnumerable<PurchaseRequest> GetAllPurchaseRequests()
    {
        return _purchaseRequestRepository.GetAll();
    }

    /// <summary>
    /// Busca um pedido pelo Id. Retorna null quando o pedido não existe.
    /// </summary>
    public PurchaseRequest? GetPurchaseRequestsById(int id)
    {
        return _purchaseRequestRepository.GetById(id);
    }

    /// <summary>
    /// Aprova a etapa atual do pedido.
    /// RN4: garante aprovação sequencial.
    /// RN6: registra aprovação e conclusão no histórico.
    /// RN7: conclui o pedido somente após todas as aprovações exigidas.
    /// </summary>
    public PurchaseRequest ApprovePurchaseRequest(int id, PurchaseRequestActionRequest approval)
    {
        var request = GetPurchaseRequestsById(id)
            ?? throw new InvalidOperationException("Pedido não encontrado.");

        if (request.PurchaseRequestStatus == PurchaseRequestStatus.Approved)
            throw new InvalidOperationException("O pedido já está aprovado.");

        if (request.PurchaseRequestStatus == PurchaseRequestStatus.InReview)
            throw new InvalidOperationException("O pedido está em revisão e não pode ser aprovado.");

        // RN4: a etapa atual é sempre a primeira etapa pendente ordenada pela sequência.
        var currentStep = _approvalStepsRepository.GetByStatus(
                request.Id,
                ApprovalStepStatus.Pending)
            ?? throw new InvalidOperationException("O pedido não possui etapa pendente de aprovação.");

        // RN4: impede que Gestor ou Diretor aprovem antes de receberem o pedido.
        if (currentStep.ApproverRole != approval.ApproverRole)
            throw new InvalidOperationException(
                $"A próxima aprovação deve ser feita por {currentStep.ApproverRole}.");

        CreateNewCurrentStep(
            approval,
            currentStep,
            ApprovalStepStatus.Approved);

        // RN6: registra a aprovação executada pelo aprovador da etapa atual.
        _purchaseRequestHistoryRepository.Insert(new PurchaseRequestHistory
        {
            PurchaseRequestId = request.Id,
            ActionType = HistoryActionType.Approved,
            PerformedBy = approval.ActionBy,
            PerformedByRole = approval.ApproverRole,
            Comments = approval.Comments ?? $"Aprovado por {approval.ApproverRole}."
        });

        var nextStep = _approvalStepsRepository.GetByStatus(
            request.Id,
            ApprovalStepStatus.Pending);

        if (nextStep is null)
        {
            // RN7: sem próximas etapas pendentes, todas as alçadas exigidas foram aprovadas.
            request.PurchaseRequestStatus = PurchaseRequestStatus.Approved;

            // RN6: registra a conclusão do ciclo de aprovação.
            _purchaseRequestHistoryRepository.Insert(new PurchaseRequestHistory
            {
                PurchaseRequestId = request.Id,
                ActionType = HistoryActionType.Completed,
                PerformedBy = approval.ActionBy,
                PerformedByRole = approval.ApproverRole,
                Comments = "Pedido aprovado em todas as alçadas."
            });
        }
        else
        {
            // RN4: move o pedido para o próximo aprovador da sequência.
            SetPendingStatusByUserRole(request, nextStep.ApproverRole);
        }

        request.UpdatedAt = DateTime.UtcNow;

        return _purchaseRequestRepository.Update(request);
    }

    /// <summary>
    /// Solicita revisão do pedido na etapa atual de aprovação.
    /// RN5: permite que o aprovador da etapa atual solicite revisão.
    /// RN6: registra a solicitação de revisão no histórico.
    /// </summary>
    public PurchaseRequest ReviewPurchaseRequest(
        int id,
        PurchaseRequestActionRequest review)
    {
        var request = GetPurchaseRequestsById(id)
            ?? throw new InvalidOperationException("Pedido não encontrado.");

        if (request.PurchaseRequestStatus == PurchaseRequestStatus.InReview)
            throw new InvalidOperationException("O pedido já está em revisão.");

        // RN5 e RN4: somente o aprovador responsável pela etapa atual pode solicitar revisão.
        var currentStep = _approvalStepsRepository.GetByStatus(
                request.Id,
                ApprovalStepStatus.Pending)
            ?? throw new InvalidOperationException("O pedido não possui etapa pendente de aprovação.");

        if (currentStep.ApproverRole != review.ApproverRole)
            throw new InvalidOperationException(
                $"A próxima aprovação deve ser feita por {currentStep.ApproverRole}.");

        // RN5: o pedido sai do fluxo de aprovação e retorna para ajuste do solicitante.
        request.PurchaseRequestStatus = PurchaseRequestStatus.InReview;

        // RN5: ao voltar de revisão, o pedido deverá percorrer a cadeia novamente.
        foreach (var step in request.ApprovalSteps)
        {
            ResetCurrentStep(step);
        }

        // RN6: registra quem solicitou a revisão e a justificativa informada.
        _purchaseRequestHistoryRepository.Insert(new PurchaseRequestHistory
        {
            PurchaseRequestId = request.Id,
            ActionType = HistoryActionType.ReviewRequested,
            PerformedBy = review.ActionBy,
            PerformedByRole = review.ApproverRole,
            Comments = review.Comments ?? $"Revisão solicitada por {review.ApproverRole}."
        });

        request.UpdatedAt = DateTime.UtcNow;

        return _purchaseRequestRepository.Update(request);
    }

    /// <summary>
    /// Reenvia um pedido que estava em revisão.
    /// RN2: recalcula o valor total após ajustes.
    /// RN3: recria as etapas conforme a nova alçada.
    /// RN5: reinicia o fluxo de aprovação desde Suprimentos.
    /// RN6: registra o reenvio no histórico.
    /// </summary>
    public PurchaseRequest ResubmitPurchaseRequest(int id, PurchaseRequest request)
    {
        var currentRequest = GetPurchaseRequestsById(id)
            ?? throw new InvalidOperationException("Pedido não encontrado.");

        if (currentRequest.PurchaseRequestStatus != PurchaseRequestStatus.InReview)
            throw new InvalidOperationException("O pedido necessita estar em revisão.");

        currentRequest.RequesterName = request.RequesterName;
        currentRequest.Items = request.Items;

        // RN2: o pedido revisado pode ter itens, quantidades ou preços alterados.
        CalculateTotalAmount(currentRequest);

        // RN3 e RN5: a nova alçada substitui a cadeia anterior e reinicia o fluxo.
        currentRequest.ApprovalSteps = CreateApprovalSteps(currentRequest);

        currentRequest.UpdatedAt = DateTime.UtcNow;

        // RN5: após reenvio, a primeira etapa volta a ser Suprimentos.
        SetPendingStatusByUserRole(currentRequest, UserRole.Supply);

        // RN6: registra o reenvio do pedido revisado.
        _purchaseRequestHistoryRepository.Insert(new PurchaseRequestHistory
        {
            PurchaseRequestId = currentRequest.Id,
            ActionType = HistoryActionType.Resubmitted,
            PerformedBy = currentRequest.RequesterName,
            PerformedByRole = UserRole.Requester,
            Comments = "Pedido revisado."
        });

        return _purchaseRequestRepository.Update(currentRequest);
    }

    /// <summary>
    /// Cancela um pedido de compra ainda não finalizado.
    /// RN8: permite cancelamento por qualquer nível de aprovação.
    /// RN6: registra o cancelamento no histórico.
    /// </summary>
    public PurchaseRequest CancelPurchaseRequest(
        int id,
        PurchaseRequestActionRequest cancellation)
    {
        var request = GetPurchaseRequestsById(id)
            ?? throw new InvalidOperationException("Pedido não encontrado.");

        if (request.PurchaseRequestStatus == PurchaseRequestStatus.Cancelled)
            throw new InvalidOperationException("O pedido já está cancelado.");

        if (request.PurchaseRequestStatus == PurchaseRequestStatus.Approved)
            throw new InvalidOperationException("O pedido já está aprovado e não pode ser cancelado.");

        // RN8: o status principal do pedido passa a indicar cancelamento.
        request.PurchaseRequestStatus = PurchaseRequestStatus.Cancelled;

        // RN8: etapas pendentes também são encerradas para não restar aprovação aberta.
        foreach (var step in _approvalStepsRepository.GetAllByStatus(
            request.Id,
            ApprovalStepStatus.Pending))
        {
            CreateNewCurrentStep(cancellation, step, ApprovalStepStatus.Cancelled);
        }

        // RN6: registra quem cancelou e a justificativa informada.
        _purchaseRequestHistoryRepository.Insert(new PurchaseRequestHistory
        {
            PurchaseRequestId = request.Id,
            ActionType = HistoryActionType.Cancelled,
            PerformedBy = cancellation.ActionBy,
            PerformedByRole = cancellation.ApproverRole,
            Comments = cancellation.Comments ?? $"Pedido cancelado por {cancellation.ApproverRole}."
        });

        request.UpdatedAt = DateTime.UtcNow;

        return _purchaseRequestRepository.Update(request);
    }

    /// <summary>
    /// RN2: calcula o valor total do pedido com base em quantidade e preço unitário.
    /// </summary>
    private static void CalculateTotalAmount(PurchaseRequest request)
    {
        request.TotalAmount = request.Items
            .Sum(item => item.Quantity * item.UnitPrice);
    }

    /// <summary>
    /// RN3: cria as etapas de aprovação exigidas pela alçada do valor total.
    /// </summary>
    private static List<ApprovalStep> CreateApprovalSteps(PurchaseRequest request)
    {
        var approverRoles = GetApprovalFlow(request.TotalAmount);

        return [.. approverRoles
            .Select((role, index) => new ApprovalStep
            {
                PurchaseRequestId = request.Id,
                ApproverRole = role,
                Sequence = index + 1,
                Status = ApprovalStepStatus.Pending
            })];
    }

    /// <summary>
    /// RN3: define quais papéis aprovam o pedido de acordo com o valor total.
    /// </summary>
    private static UserRole[] GetApprovalFlow(decimal totalAmount)
    {
        return totalAmount switch
        {
            <= 100 => [UserRole.Supply],
            <= 1000 => [UserRole.Supply, UserRole.Manager],
            _ => [UserRole.Supply, UserRole.Manager, UserRole.Director]
        };
    }

    /// <summary>
    /// RN4: atualiza o status do pedido para indicar o próximo perfil aprovador.
    /// </summary>
    private static void SetPendingStatusByUserRole(PurchaseRequest request, UserRole userRole)
    {
        request.PurchaseRequestStatus = userRole switch
        {
            UserRole.Supply => PurchaseRequestStatus.PendingSupplyApproval,
            UserRole.Manager => PurchaseRequestStatus.PendingManagerApproval,
            UserRole.Director => PurchaseRequestStatus.PendingDirectorApproval,
            _ => request.PurchaseRequestStatus
        };
    }

    /// <summary>
    /// Atualiza uma etapa de aprovação com a ação executada pelo usuário.
    /// Usado em aprovação e cancelamento.
    /// </summary>
    private static void CreateNewCurrentStep(
        PurchaseRequestActionRequest actionRequest,
        ApprovalStep currentStep,
        ApprovalStepStatus status)
    {
        currentStep.Status = status;
        currentStep.ActionBy = actionRequest.ActionBy;
        currentStep.ActionAt = DateTime.UtcNow;
        currentStep.Comments = actionRequest.Comments;
    }

    /// <summary>
    /// RN5: limpa uma etapa para que o pedido possa reiniciar a aprovação após revisão.
    /// </summary>
    private static void ResetCurrentStep(ApprovalStep step)
    {
        step.Status = ApprovalStepStatus.Pending;
        step.ActionBy = null;
        step.ActionAt = null;
        step.Comments = null;
    }
}
