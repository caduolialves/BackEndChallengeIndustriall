using PurchaseOrderChallenge.Models.Enums;

namespace PurchaseOrderChallenge.Models;

public class ApprovalStep
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public UserRole ApproverRole { get; set; }
    public int Sequence { get; set; }
    public ApprovalStepStatus Status { get; set; }
    public string? ActionBy { get; set; }
    public DateTime? ActionAt { get; set; }
    public string? Comments { get; set; }
}