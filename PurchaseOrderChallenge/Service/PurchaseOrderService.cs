using PurchaseOrderChallenge.Models;
using PurchaseOrderChallenge.Models.Enums;
using PurchaseOrderChallenge.Models.DTOs;

namespace PurchaseOrderChallenge.Service;

public class PurchaseOrderService
{
    private static readonly List<PurchaseRequest> _orders = new();
    public void CreatePurchaseRequest(PurchaseRequest request)
    {
        CalculateTotalAmount(request);

        request.Id = request.Id == 0 ? GetNextId() : request.Id;
        request.CreatedAt = DateTime.UtcNow;
        request.UpdatedAt = request.CreatedAt;
        request.ApprovalSteps = CreateApprovalSteps(request);

        SetPendingStatusByUserRole(request, UserRole.Supply);
        AddHistory(request, HistoryActionType.Created, request.RequesterName, UserRole.Requester, "Pedido criado.");

        _orders.Add(request);
    }

    public IEnumerable<PurchaseRequest> GetAllPurchaseRequests()
    {
        return _orders;
    }

    public PurchaseRequest? GetPurchaseRequestsById(int id)
    {
        return _orders.FirstOrDefault(x => x.Id == id);
    }


    public PurchaseRequest ApprovePurchaseRequest(int id, PurchaseRequestActionRequest approval)
    {
        var request = GetPurchaseRequestsById(id) ?? throw new InvalidOperationException("Pedido não encontrado.");

        // Validação para garantir que um pedido já aprovado não possa ser aprovado novamente, caso contrário retorna BadRequest.
        if (request.PurchaseRequestStatus == PurchaseRequestStatus.Approved)
            throw new InvalidOperationException("O pedido já está aprovado.");

        // Validação para garantir que um pedido em revisão não possa ser aprovado, caso contrário retorna BadRequest.
        if (request.PurchaseRequestStatus == PurchaseRequestStatus.InReview)
            throw new InvalidOperationException("O pedido está em revisão e não pode ser aprovado.");

        // Validação para garantir que um pedido em revisão não possa ser aprovado, caso contrário retorna BadRequest.
        var currentStep = request.ApprovalSteps
            .Where(step => step.Status == ApprovalStepStatus.Pending)
            .OrderBy(step => step.Sequence)
            .FirstOrDefault() ?? throw new InvalidOperationException("O pedido não possui etapa pendente de aprovação.");

        // RN4: Validação para garantir que a aprovação seja feita na ordem correta das etapas, caso contrário retorna BadRequest.
        if (currentStep.ApproverRole != approval.ApproverRole)
            throw new InvalidOperationException($"A próxima aprovação deve ser feita por {currentStep.ApproverRole}.");

        currentStep.Status = ApprovalStepStatus.Approved;
        currentStep.ActionBy = approval.ActionBy;
        currentStep.ActionAt = DateTime.UtcNow;
        currentStep.Comments = approval.Comments;

        AddHistory(
            request,
            HistoryActionType.Approved,
            approval.ActionBy,
            approval.ApproverRole,
            approval.Comments ?? $"Aprovado por {approval.ApproverRole}.");

        // Atualização do status do pedido com base na etapa de aprovação atual, caso contrário retorna BadRequest.
        var nextStep = request.ApprovalSteps
            .Where(step => step.Status == ApprovalStepStatus.Pending)
            .OrderBy(step => step.Sequence)
            .FirstOrDefault();

        // Atualização do status do pedido para "Aprovado" quando todas as etapas de aprovação forem concluídas, caso contrário retorna BadRequest.
        if (nextStep is null)
        {
            request.PurchaseRequestStatus = PurchaseRequestStatus.Approved;
            AddHistory(request, HistoryActionType.Completed, approval.ActionBy, approval.ApproverRole, "Pedido aprovado em todas as alçadas.");
        }
        else
        {
            SetPendingStatusByUserRole(request, nextStep.ApproverRole);
        }

        request.UpdatedAt = DateTime.UtcNow;
        return request;
    }

    public PurchaseRequest ReviewPurchaseRequest(int id, PurchaseRequestActionRequest review)
    {
        var request = GetPurchaseRequestsById(id) ?? throw new InvalidOperationException("Pedido não encontrado.");

        if (request.PurchaseRequestStatus == PurchaseRequestStatus.InReview)
            throw new InvalidOperationException("O pedido já está em revisão.");

        var currentStep = request.ApprovalSteps
            .Where(step => step.Status == ApprovalStepStatus.Pending)
            .OrderBy(step => step.Sequence)
            .FirstOrDefault() ?? throw new InvalidOperationException("O pedido não possui etapa pendente de aprovação.");

        if (currentStep.ApproverRole != review.ApproverRole)
            throw new InvalidOperationException($"A próxima aprovação deve ser feita por {currentStep.ApproverRole}.");

        request.PurchaseRequestStatus = PurchaseRequestStatus.InReview;
        
        foreach (var step in request.ApprovalSteps)
        {
            currentStep.Status = ApprovalStepStatus.Pending;
            currentStep.ActionBy = null;
            currentStep.ActionAt = null;
            currentStep.Comments = null;
        }

        AddHistory(
            request,
            HistoryActionType.ReviewRequested,
            review.ActionBy,
            review.ApproverRole,
            review.Comments ?? $"Revisão solicitada por {review.ApproverRole}.");
        
        request.UpdatedAt = DateTime.UtcNow;
        return request;
    }

    // RN2: Calculo do valor total do pedido com base nos itens, quantidades e precos unitarios.
    private static void CalculateTotalAmount(PurchaseRequest request)
    {
        request.TotalAmount = request.Items.Sum(item => item.Quantity * item.UnitPrice);
    }

    private static List<ApprovalStep> CreateApprovalSteps(PurchaseRequest request)
    {
        var approverRoles = GetApprovalFlow(request.TotalAmount);

        return approverRoles
            .Select((role, index) => new ApprovalStep
            {
                Id = index + 1,
                PurchaseOrderId = request.Id,
                ApproverRole = role,
                Sequence = index + 1,
                Status = ApprovalStepStatus.Pending
            })
            .ToList();
    }

    private static UserRole[] GetApprovalFlow(decimal totalAmount)
    {
        if (totalAmount <= 100)
            return [UserRole.Supply];

        if (totalAmount <= 1000)
            return [UserRole.Supply, UserRole.Manager];

        return [UserRole.Supply, UserRole.Manager, UserRole.Director];
    }

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

    private static void AddHistory(
        PurchaseRequest request,
        HistoryActionType actionType,
        string performedBy,
        UserRole performedByRole,
        string comments)
    {
        request.History.Add(new PurchaseRequestHistory
        {
            Id = request.History.Count + 1,
            PurchaseRequestId = request.Id,
            ActionType = actionType,
            PerformedBy = performedBy,
            PerformedByRole = performedByRole,
            Comments = comments,
            CreatedAt = DateTime.UtcNow
        });
    }

    private static int GetNextId()
    {
        return _orders.Count == 0 ? 1 : _orders.Max(order => order.Id) + 1;
    }

}
