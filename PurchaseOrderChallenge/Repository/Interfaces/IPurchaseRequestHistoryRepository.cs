using PurchaseOrderChallenge.Models;

namespace PurchaseOrderChallenge.Repository.Interfaces;

public interface IPurchaseRequestHistoryRepository
{
    void Insert(PurchaseRequestHistory history);
}
