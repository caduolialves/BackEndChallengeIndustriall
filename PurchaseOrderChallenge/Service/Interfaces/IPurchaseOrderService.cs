using PurchaseOrderChallenge.Models;
using PurchaseOrderChallenge.Models.DTOs;

namespace PurchaseOrderChallenge.Service.Interfaces;

public interface IPurchaseOrderService
{
    void CreatePurchaseRequest(PurchaseRequest request);
    IEnumerable<PurchaseRequest> GetAllPurchaseRequests();
    PurchaseRequest? GetPurchaseRequestsById(int id);
    PurchaseRequest ApprovePurchaseRequest(int id, PurchaseRequestActionRequest approval);
    PurchaseRequest ReviewPurchaseRequest(int id, PurchaseRequestActionRequest review);
    PurchaseRequest ResubmitPurchaseRequest(int id, PurchaseRequest request);
    PurchaseRequest CancelPurchaseRequest(int id, PurchaseRequestActionRequest cancellation);
}
