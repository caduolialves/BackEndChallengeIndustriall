using PurchaseOrderChallenge.Models;

namespace PurchaseOrderChallenge.Repository.Interfaces;

public interface IPurchaseRequestRepository
{
    IEnumerable<PurchaseRequest> GetAll();
    PurchaseRequest? GetById(int id);
    PurchaseRequest Insert(PurchaseRequest request);
    PurchaseRequest Update(PurchaseRequest request);
}
