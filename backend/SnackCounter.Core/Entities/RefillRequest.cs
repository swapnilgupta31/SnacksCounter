using SnackCounter.Core.Enums;

namespace SnackCounter.Core.Entities;

public class RefillRequest
{
    public int Id { get; set; }
    public int SnackBoxId { get; set; }
    public int OfficeId { get; set; }
    public RefillStatus Status { get; set; } = RefillStatus.Requested;
    public int? AssignedVolunteerId { get; set; }
    public decimal EstimatedCost { get; set; }
    public string? Notes { get; set; }
    public int ReportedByEmployeeId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? OfferedAt { get; set; }
    public DateTime? OfferExpiresAt { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? AssignmentDeadline { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CancellationReason { get; set; }

    public SnackBox SnackBox { get; set; } = null!;
    public Office Office { get; set; } = null!;
    public Employee? AssignedVolunteer { get; set; }
    public Employee ReportedBy { get; set; } = null!;
    public Purchase? Purchase { get; set; }
    public ReimbursementClaim? ReimbursementClaim { get; set; }
    public BudgetReservation? BudgetReservation { get; set; }
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
