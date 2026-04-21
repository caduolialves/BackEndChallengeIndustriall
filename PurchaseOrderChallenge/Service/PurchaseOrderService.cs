using PurchaseOrderChallenge.Models;
using PurchaseOrderChallenge.Models.Enums;
using PurchaseOrderChallenge.Models.DTOs;
using PurchaseOrderChallenge.Repository.Interfaces;
using PurchaseOrderChallenge.Service.Interfaces;

namespace PurchaseOrderChallenge.Service;

public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly IPurchaseRequestRepository _purchaseRequestRepository;
    private readonly IApprovalStepsRepository _approvalStepsRepository;

    private readonly IPurchaseRequestHistoryRepository _purchaseRequestHistoryRepository;

    public PurchaseOrderService(IPurchaseRequestRepository repository, IApprovalStepsRepository approvalStepsRepository, IPurchaseRequestHistoryRepository purchaseRequestHistoryRepository)
    {
        _purchaseRequestRepository = repository;
        _approvalStepsRepository = approvalStepsRepository;
        _purchaseRequestHistoryRepository = purchaseRequestHistoryRepository;
    }

    /// <summary>
    /// Cria um novo pedido de compra, calcula o total, define as etapas de aprovação
    /// conforme a alçada e inicia o fluxo pendente em Suprimentos.
    /// </summary>
    public void CreatePurchaseRequest(PurchaseRequest request)
    {
        // O total precisa existir antes da criação das etapas, pois a alçada depende dele.
        CalculateTotalAmount(request);

        request.CreatedAt = DateTime.UtcNow;
        request.UpdatedAt = request.CreatedAt;

        // Cria a cadeia de aprovação: Suprimentos, Gestor e/ou Diretor conforme o valor.
        request.ApprovalSteps = CreateApprovalSteps(request);

        // Todo pedido começa aguardando a primeira aprovação: Suprimentos.
        SetPendingStatusByUserRole(request, UserRole.Supply);

        var insertedRequest = _purchaseRequestRepository.Insert(request);

        // Registra a criação do pedido no histórico para rastreabilidade.
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
    /// Retorna todos os pedidos armazenados em memória.
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
    /// Aprova a etapa atual do pedido e avança o fluxo para o próximo aprovador.
    /// A aprovação respeita a sequência definida pela alçada.
    /// </summary>
    public PurchaseRequest ApprovePurchaseRequest(int id, PurchaseRequestActionRequest approval)
    {
        // Localiza o pedido antes de validar qualquer regra de aprovação.
        var request = GetPurchaseRequestsById(id) ?? throw new InvalidOperationException("Pedido não encontrado.");

        // Impede aprovar novamente um pedido cujo fluxo já foi concluído.
        if (request.PurchaseRequestStatus == PurchaseRequestStatus.Approved)
            throw new InvalidOperationException("O pedido já está aprovado.");

        // Um pedido em revisão precisa ser ajustado antes de voltar para a aprovação.
        if (request.PurchaseRequestStatus == PurchaseRequestStatus.InReview)
            throw new InvalidOperationException("O pedido está em revisão e não pode ser aprovado.");

        // RN4: a etapa atual é sempre a primeira pendente na sequência de aprovação.
        var currentStep = _approvalStepsRepository.GetByStatus(request.Id, ApprovalStepStatus.Pending) ?? throw new InvalidOperationException("O pedido não possui etapa pendente de aprovação.");

        // Garante que o aprovador informado corresponde exatamente à etapa atual.
        if (currentStep.ApproverRole != approval.ApproverRole)
            throw new InvalidOperationException($"A próxima aprovação deve ser feita por {currentStep.ApproverRole}.");

        // Marca a etapa atual como aprovada e registra quem executou a ação.
        CreateNewCurrentStep(approval, currentStep, ApprovalStepStatus.Approved);

        // Mantém um histórico separado das etapas para auditoria do pedido.
        _purchaseRequestHistoryRepository.Insert(new PurchaseRequestHistory
        {
            PurchaseRequestId = request.Id,
            ActionType = HistoryActionType.Approved,
            PerformedBy = approval.ActionBy,
            PerformedByRole = approval.ApproverRole,
            Comments = approval.Comments ?? $"Aprovado por {approval.ApproverRole}."
        });

        // Depois da aprovação, verifica se ainda existe alguma etapa pendente.
        var nextStep = _approvalStepsRepository.GetByStatus(request.Id, ApprovalStepStatus.Pending);

        // Se não houver próxima etapa, todas as alçadas exigidas já aprovaram o pedido.
        if (nextStep is null)
        {
            request.PurchaseRequestStatus = PurchaseRequestStatus.Approved;
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
            // Caso contrário, o pedido passa a aguardar o próximo aprovador da sequência.
            SetPendingStatusByUserRole(request, nextStep.ApproverRole);
        }

        request.UpdatedAt = DateTime.UtcNow;

        return _purchaseRequestRepository.Update(request);
    }

    /// <summary>
    /// Solicita revisão do pedido na etapa atual de aprovação.
    /// O pedido sai temporariamente do fluxo até ser reapresentado.
    /// </summary>
    public PurchaseRequest ReviewPurchaseRequest(int id, PurchaseRequestActionRequest review)
    {
        // Busca o pedido antes de aplicar as regras de solicitação de revisão.
        var request = GetPurchaseRequestsById(id) ?? throw new InvalidOperationException("Pedido não encontrado.");

        // Evita solicitar revisão repetida para um pedido que já está nesse estado.
        if (request.PurchaseRequestStatus == PurchaseRequestStatus.InReview)
            throw new InvalidOperationException("O pedido já está em revisão.");

        // A revisão também respeita a sequência: só o responsável pela etapa atual pode solicitá-la.
        var currentStep = _approvalStepsRepository.GetByStatus(request.Id, ApprovalStepStatus.Pending) ?? throw new InvalidOperationException("O pedido não possui etapa pendente de aprovação.");

        // Bloqueia revisão por um papel que ainda não recebeu o pedido.
        if (currentStep.ApproverRole != review.ApproverRole)
            throw new InvalidOperationException($"A próxima aprovação deve ser feita por {currentStep.ApproverRole}.");

        // O status InReview sinaliza que o pedido saiu temporariamente do fluxo de aprovação.
        request.PurchaseRequestStatus = PurchaseRequestStatus.InReview;
        
        // Remove decisões anteriores das etapas para que o fluxo recomece após a correção.
        foreach (var step in request.ApprovalSteps)
        {
            ResetCurrentStep(step);
        }

        // Registra quem pediu a revisão e a justificativa informada.
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
    /// Reapresenta um pedido que estava em revisão, atualizando seus dados editáveis,
    /// recalculando o total e reiniciando a aprovação por Suprimentos.
    /// </summary>
    public PurchaseRequest ResubmitPurchaseRequest(int id, PurchaseRequest request)
    {
        // Busca o pedido original para preservar histórico e dados do fluxo.
        var currentRequest = GetPurchaseRequestsById(id) ?? throw new InvalidOperationException("Pedido não encontrado.");

        if (currentRequest.PurchaseRequestStatus != PurchaseRequestStatus.InReview)
            throw new InvalidOperationException("O pedido necessita estar em revisão.");

        // Atualiza somente os dados editáveis enviados na reapresentação,
        // sem perder histórico, datas originais e demais dados do fluxo.
        currentRequest.RequesterName = request.RequesterName;
        currentRequest.Items = request.Items;

        CalculateTotalAmount(currentRequest);

        // O novo valor pode mudar a alçada, por isso as etapas são recriadas.
        currentRequest.ApprovalSteps = CreateApprovalSteps(currentRequest);

        currentRequest.UpdatedAt = DateTime.UtcNow;

        SetPendingStatusByUserRole(currentRequest, UserRole.Supply);

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
    /// Cancela um pedido ainda não aprovado, marca as etapas pendentes como canceladas
    /// e registra o cancelamento no histórico.
    /// </summary>
    public PurchaseRequest CancelPurchaseRequest(int id, PurchaseRequestActionRequest cancellation)
    {
        // Localiza o pedido antes de validar as regras de cancelamento.
        var request = GetPurchaseRequestsById(id) ?? throw new InvalidOperationException("Pedido não encontrado.");

        // Evita registrar o mesmo cancelamento mais de uma vez.
        if (request.PurchaseRequestStatus == PurchaseRequestStatus.Cancelled)
            throw new InvalidOperationException("O pedido já está cancelado.");

        // Pela regra atual, um pedido totalmente aprovado não pode mais ser cancelado.
        if (request.PurchaseRequestStatus == PurchaseRequestStatus.Approved)
            throw new InvalidOperationException("O pedido já está aprovado e não pode ser cancelado.");

        // O status principal do pedido passa a indicar o encerramento por cancelamento.
        request.PurchaseRequestStatus = PurchaseRequestStatus.Cancelled;

        // Etapas ainda pendentes também são encerradas para não restar aprovação aberta.


        foreach (var step in _approvalStepsRepository.GetAllByStatus(request.Id, ApprovalStepStatus.Pending))
        {
            CreateNewCurrentStep(cancellation, step, ApprovalStepStatus.Cancelled);
        }

        // Registra quem cancelou e a justificativa informada.        
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
    /// RN2: Calcula o valor total do pedido com base na quantidade e preço unitário dos itens.
    /// </summary>
    private static void CalculateTotalAmount(PurchaseRequest request)
    {
        request.TotalAmount = request.Items.Sum(item => item.Quantity * item.UnitPrice);
    }

    /// <summary>
    /// Cria as etapas de aprovação do pedido conforme a regra de alçada calculada pelo valor total.
    /// </summary>
    private static List<ApprovalStep> CreateApprovalSteps(PurchaseRequest request)
    {
        // Primeiro descobre quais papéis precisam aprovar este pedido.
        var approverRoles = GetApprovalFlow(request.TotalAmount);

        // Depois transforma cada papel em uma etapa sequencial pendente.
        return approverRoles
            .Select((role, index) => new ApprovalStep
            {
                PurchaseRequestId = request.Id,
                ApproverRole = role,
                Sequence = index + 1,
                Status = ApprovalStepStatus.Pending
            })
            .ToList();
    }

    /// <summary>
    /// Define quais papéis devem aprovar o pedido conforme o valor total.
    /// </summary>
    private static UserRole[] GetApprovalFlow(decimal totalAmount)
    {
        // Até R$ 100,00: somente Suprimentos aprova.
        if (totalAmount <= 100)
            return [UserRole.Supply];

        // Acima de R$ 100,00 e até R$ 1.000,00: Suprimentos e Gestor aprovam.
        if (totalAmount <= 1000)
            return [UserRole.Supply, UserRole.Manager];

        // Acima de R$ 1.000,00: Suprimentos, Gestor e Diretor aprovam.
        return [UserRole.Supply, UserRole.Manager, UserRole.Director];
    }

    /// <summary>
    /// Atualiza o status do pedido para indicar qual perfil deve aprovar em seguida.
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

    private static void CreateNewCurrentStep(PurchaseRequestActionRequest actionRequest, ApprovalStep currentStep, ApprovalStepStatus status)
    {
        currentStep.Status = status;
        currentStep.ActionBy = actionRequest.ActionBy;
        currentStep.ActionAt = DateTime.UtcNow;
        currentStep.Comments = actionRequest.Comments;
    }

    private static void ResetCurrentStep(ApprovalStep step)
    {
        step.Status = ApprovalStepStatus.Pending;
        step.ActionBy = null;
        step.ActionAt = null;
        step.Comments = null;
    }
}
