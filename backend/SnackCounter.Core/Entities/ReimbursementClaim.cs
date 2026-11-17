using SnackCounter.Core.Enums;

namespace SnackCounter.Core.Entities;

public class ReimbursementClaim
{
    public int Id { get; set; }
    public int RefillRequestId { get; set; }
    public int EmployeeId { get; set; }
    public string? ExternalClaimId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public ClaimStatus Status { get; set; } = ClaimStatus.PendingSubmission;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime? PaidAt { get; set; }

    public RefillRequest RefillRequest { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
}
