namespace PurchaseOrderChallenge.Models.Enums;

public enum PurchaseRequestStatus
{
    PendingSupplyApproval,
    PendingManagerApproval,
    PendingDirectorApproval,
    InReview,
    Approved,
    Cancelled
}