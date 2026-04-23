using PurchaseOrderChallenge.Models;

namespace PurchaseOrderChallenge.Repository.Interfaces;

public interface IPurchaseRequestHistoryRepository
{
    /// <summary>
    /// Insere um registro de histórico do pedido.
    /// </summary>
    void Insert(PurchaseRequestHistory history);
}
