namespace PurchaseOrderChallenge.Models;

public class PurchaseRequestItem
{
    public int Id { get; set; }
    public int PurchaseRequestId { get; set; }
    public PurchaseRequest? PurchaseRequest { get; set; }
    public required string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
