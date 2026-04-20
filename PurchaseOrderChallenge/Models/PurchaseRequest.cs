using PurchaseOrderChallenge.Models.Enums;

namespace PurchaseOrderChallenge.Models;

public class PurchaseRequest
{
    public int Id { get; set; }
    public required string RequesterName { get; set; }
    public decimal TotalAmount { get; set; }
    public PurchaseRequestStatus PurchaseRequestStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public required ICollection<PurchaseRequestItem> Items { get; set; }
    public ICollection<ApprovalStep> ApprovalSteps { get; set; } = [];
    public ICollection<PurchaseRequestHistory> History { get; set; } = [];
}
