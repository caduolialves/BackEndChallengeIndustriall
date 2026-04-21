using PurchaseOrderChallenge.Models;
using PurchaseOrderChallenge.Models.Enums;
using PurchaseOrderChallenge.Models.DTOs;

namespace PurchaseOrderChallenge.Service;

public class PurchaseOrderService
{
    private static readonly List<PurchaseRequest> _orders = new();

    /// <summary>
    /// Cria um novo pedido de compra, calcula o total, define as etapas de aprovação
    /// conforme a alçada e inicia o fluxo pendente em Suprimentos.
    /// </summary>
    public void CreatePurchaseRequest(PurchaseRequest request)
    {
        // O total precisa existir antes da criação das etapas, pois a alçada depende dele.
        CalculateTotalAmount(request);

        // Como os dados estão em memória, o serviço simula a geração automática do Id.
        request.Id = request.Id == 0 ? GetNextId() : request.Id;
        request.CreatedAt = DateTime.UtcNow;
        request.UpdatedAt = request.CreatedAt;

        // Cria a cadeia de aprovação: Suprimentos, Gestor e/ou Diretor conforme o valor.
        request.ApprovalSteps = CreateApprovalSteps(request);

        // Todo pedido começa aguardando a primeira aprovação: Suprimentos.
        SetPendingStatusByUserRole(request, UserRole.Supply);

        // Registra a criação do pedido no histórico para rastreabilidade.
        AddHistory(request, HistoryActionType.Created, request.RequesterName, UserRole.Requester, "Pedido criado.");

        _orders.Add(request);
    }

    /// <summary>
    /// Retorna todos os pedidos armazenados em memória.
    /// </summary>
    public IEnumerable<PurchaseRequest> GetAllPurchaseRequests()
    {
        return _orders;
    }

    /// <summary>
    /// Busca um pedido pelo Id. Retorna null quando o pedido não existe.
    /// </summary>
    public PurchaseRequest? GetPurchaseRequestsById(int id)
    {
        return _orders.FirstOrDefault(x => x.Id == id);
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
        var currentStep = request.ApprovalSteps
            .Where(step => step.Status == ApprovalStepStatus.Pending)
            .OrderBy(step => step.Sequence)
            .FirstOrDefault() ?? throw new InvalidOperationException("O pedido não possui etapa pendente de aprovação.");

        // Garante que o aprovador informado corresponde exatamente à etapa atual.
        if (currentStep.ApproverRole != approval.ApproverRole)
            throw new InvalidOperationException($"A próxima aprovação deve ser feita por {currentStep.ApproverRole}.");

        // Marca a etapa atual como aprovada e registra quem executou a ação.
        currentStep.Status = ApprovalStepStatus.Approved;
        currentStep.ActionBy = approval.ActionBy;
        currentStep.ActionAt = DateTime.UtcNow;
        currentStep.Comments = approval.Comments;

        // Mantém um histórico separado das etapas para auditoria do pedido.
        AddHistory(
            request,
            HistoryActionType.Approved,
            approval.ActionBy,
            approval.ApproverRole,
            approval.Comments ?? $"Aprovado por {approval.ApproverRole}.");

        // Depois da aprovação, verifica se ainda existe alguma etapa pendente.
        var nextStep = request.ApprovalSteps
            .Where(step => step.Status == ApprovalStepStatus.Pending)
            .OrderBy(step => step.Sequence)
            .FirstOrDefault();

        // Se não houver próxima etapa, todas as alçadas exigidas já aprovaram o pedido.
        if (nextStep is null)
        {
            request.PurchaseRequestStatus = PurchaseRequestStatus.Approved;
            AddHistory(request, HistoryActionType.Completed, approval.ActionBy, approval.ApproverRole, "Pedido aprovado em todas as alçadas.");
        }
        else
        {
            // Caso contrário, o pedido passa a aguardar o próximo aprovador da sequência.
            SetPendingStatusByUserRole(request, nextStep.ApproverRole);
        }

        request.UpdatedAt = DateTime.UtcNow;
        return request;
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
        var currentStep = request.ApprovalSteps
            .Where(step => step.Status == ApprovalStepStatus.Pending)
            .OrderBy(step => step.Sequence)
            .FirstOrDefault() ?? throw new InvalidOperationException("O pedido não possui etapa pendente de aprovação.");

        // Bloqueia revisão por um papel que ainda não recebeu o pedido.
        if (currentStep.ApproverRole != review.ApproverRole)
            throw new InvalidOperationException($"A próxima aprovação deve ser feita por {currentStep.ApproverRole}.");

        // O status InReview sinaliza que o pedido saiu temporariamente do fluxo de aprovação.
        request.PurchaseRequestStatus = PurchaseRequestStatus.InReview;
        
        // Remove decisões anteriores das etapas para que o fluxo recomece após a correção.
        foreach (var step in request.ApprovalSteps)
        {
            step.Status = ApprovalStepStatus.Pending;
            step.ActionBy = null;
            step.ActionAt = null;
            step.Comments = null;
        }

        // Registra quem pediu a revisão e a justificativa informada.
        AddHistory(
            request,
            HistoryActionType.ReviewRequested,
            review.ActionBy,
            review.ApproverRole,
            review.Comments ?? $"Revisão solicitada por {review.ApproverRole}.");
        
        request.UpdatedAt = DateTime.UtcNow;
        return request;
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
        AddHistory(currentRequest, HistoryActionType.Resubmitted, currentRequest.RequesterName, UserRole.Requester, "Pedido revisado.");

        return currentRequest;
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
        foreach (var step in request.ApprovalSteps.Where(step => step.Status == ApprovalStepStatus.Pending))
        {
            step.Status = ApprovalStepStatus.Cancelled;
            step.ActionBy = cancellation.ActionBy;
            step.ActionAt = DateTime.UtcNow;
            step.Comments = cancellation.Comments;
        }

        // Registra quem cancelou e a justificativa informada.
        AddHistory(
            request,
            HistoryActionType.Cancelled,
            cancellation.ActionBy,
            cancellation.ApproverRole,
            cancellation.Comments ?? $"Pedido cancelado por {cancellation.ApproverRole}.");

        request.UpdatedAt = DateTime.UtcNow;

        return request;
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
                Id = index + 1,
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

    /// <summary>
    /// Adiciona um registro de auditoria ao histórico do pedido.
    /// </summary>
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

    /// <summary>
    /// Gera o próximo Id para pedidos armazenados em memória.
    /// </summary>
    private static int GetNextId()
    {
        // Simula o autoincremento que normalmente seria feito por um banco de dados.
        return _orders.Count == 0 ? 1 : _orders.Max(order => order.Id) + 1;
    }

}
