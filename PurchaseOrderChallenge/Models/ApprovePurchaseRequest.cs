using PurchaseOrderChallenge.Models.Enums;

namespace PurchaseOrderChallenge.Models;

public class ApprovePurchaseRequest
{
    public UserRole ApproverRole { get; set; }
    public required string ActionBy { get; set; }
    public string? Comments { get; set; }
}
