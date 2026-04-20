using PurchaseOrderChallenge.Models.Enums;

namespace PurchaseOrderChallenge.Models;

public class PurchaseRequestHistory
{
    public int Id { get; set; }
    public int PurchaseRequestId { get; set; }
    public HistoryActionType ActionType { get; set; }
    public required string PerformedBy { get; set; }
    public UserRole PerformedByRole { get; set; }

    public required string Comments { get; set; }
    public DateTime CreatedAt { get; set; }
}