namespace SnackCounter.Core.Entities;

public class Purchase
{
    public int Id { get; set; }
    public int RefillRequestId { get; set; }
    public int EmployeeId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;
    public string? ReceiptReference { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsOverBudget { get; set; }

    public RefillRequest RefillRequest { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
}
