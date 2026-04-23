using PurchaseOrderChallenge.Models;
using PurchaseOrderChallenge.Models.DTOs;

namespace PurchaseOrderChallenge.Service.Interfaces;

public interface IPurchaseOrderService
{
    /// <summary>
    /// Cria um pedido e aplica RN2, RN3 e RN6 no serviço.
    /// </summary>
    void CreatePurchaseRequest(PurchaseRequest request);

    /// <summary>
    /// Retorna todos os pedidos cadastrados.
    /// </summary>
    IEnumerable<PurchaseRequest> GetAllPurchaseRequests();

    /// <summary>
    /// Busca um pedido pelo Id.
    /// </summary>
    PurchaseRequest? GetPurchaseRequestsById(int id);

    /// <summary>
    /// Aprova a etapa atual e aplica RN4, RN6 e RN7 no serviço.
    /// </summary>
    PurchaseRequest ApprovePurchaseRequest(int id, PurchaseRequestActionRequest approval);

    /// <summary>
    /// Solicita revisão e aplica RN5 e RN6 no serviço.
    /// </summary>
    PurchaseRequest ReviewPurchaseRequest(int id, PurchaseRequestActionRequest review);

    /// <summary>
    /// Reenvia pedido revisado e aplica RN2, RN3, RN5 e RN6 no serviço.
    /// </summary>
    PurchaseRequest ResubmitPurchaseRequest(int id, PurchaseRequest request);

    /// <summary>
    /// Cancela pedido e aplica RN8 e RN6 no serviço.
    /// </summary>
    PurchaseRequest CancelPurchaseRequest(int id, PurchaseRequestActionRequest cancellation);
}
