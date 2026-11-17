namespace SnackCounter.Core.Enums;

public enum RefillStatus
{
    Requested,
    Offered,
    Assigned,
    Purchased,
    Refilled,
    ClaimSubmitted,
    Reimbursed,
    Cancelled
}

public enum ClaimStatus
{
    PendingSubmission,
    Submitted,
    Approved,
    Rejected,
    Paid,
    SubmissionFailed
}

public enum ReservationStatus
{
    Active,
    Released,
    Converted
}

public enum SnackAvailability
{
    Available,
    Empty,
    RefillPending
}
