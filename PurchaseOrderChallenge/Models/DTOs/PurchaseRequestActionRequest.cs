using PurchaseOrderChallenge.Models.Enums;

namespace PurchaseOrderChallenge.Models.DTOs;

public class PurchaseRequestActionRequest
{
    public UserRole ApproverRole { get; set; }
    public required string ActionBy { get; set; }
    public string? Comments { get; set; }
}
