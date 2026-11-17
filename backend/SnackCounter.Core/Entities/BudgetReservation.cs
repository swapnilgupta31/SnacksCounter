using SnackCounter.Core.Enums;

namespace SnackCounter.Core.Entities;

public class BudgetReservation
{
    public int Id { get; set; }
    public int RefillRequestId { get; set; }
    public int OfficeId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string BudgetMonth { get; set; } = string.Empty; // "YYYY-MM"
    public ReservationStatus Status { get; set; } = ReservationStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReleasedAt { get; set; }

    public RefillRequest RefillRequest { get; set; } = null!;
    public Office Office { get; set; } = null!;
}
