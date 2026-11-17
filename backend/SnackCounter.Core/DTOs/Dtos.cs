using SnackCounter.Core.Enums;

namespace SnackCounter.Core.DTOs;

// Office DTOs
public record OfficeDto(int Id, string Name, string Currency, decimal MonthlyBudget, string TimeZoneId);

public record OfficeBudgetDto(
    int OfficeId,
    string OfficeName,
    string Currency,
    decimal MonthlyBudget,
    decimal ActualSpending,
    decimal ActiveReservations,
    decimal AvailableBudget,
    string BudgetMonth
);

// Employee DTOs
public record EmployeeDto(
    int Id, string Name, string Email, int HomeOfficeId, string HomeOfficeName,
    int CurrentOfficeId, string CurrentOfficeName, string PayrollCurrency,
    bool IsVolunteer, bool IsAvailable, bool IsOnLeave, bool IsPresentToday, string Role
);

public record UpdateAvailabilityRequest(bool IsAvailable, bool IsOnLeave, bool IsPresentToday);

// SnackBox DTOs
public record SnackBoxDto(
    int Id, int OfficeId, string OfficeName, string Name, string Category,
    string AvailabilityStatus, DateTime? LastRefilledAt, int? ActiveRefillId
);

// RefillRequest DTOs
public record RefillRequestDto(
    int Id, int SnackBoxId, string SnackBoxName, string SnackCategory,
    int OfficeId, string OfficeName, string Status,
    int? AssignedVolunteerId, string? AssignedVolunteerName,
    decimal EstimatedCost, string Currency,
    int ReportedByEmployeeId, string ReportedByName,
    DateTime CreatedAt, DateTime? OfferExpiresAt,
    DateTime? AssignedAt, DateTime? AssignmentDeadline, DateTime? CompletedAt,
    string? Notes, string? CancellationReason,
    PurchaseDto? Purchase, ReimbursementClaimDto? ReimbursementClaim
);

public record ReportEmptyRequest(decimal EstimatedCost, string? Notes);

public record SubmitPurchaseRequest(decimal Amount, string? ReceiptReference, string Description);

public record CancelRefillRequest(string Reason);

// Purchase DTOs
public record PurchaseDto(
    int Id, int RefillRequestId, int EmployeeId, string EmployeeName,
    decimal Amount, string Currency, DateTime PurchasedAt,
    string? ReceiptReference, string Description, bool IsOverBudget
);

// ReimbursementClaim DTOs
public record ReimbursementClaimDto(
    int Id, int RefillRequestId, int EmployeeId, string EmployeeName,
    string? ExternalClaimId, decimal Amount, string Currency,
    string Status, DateTime CreatedAt, DateTime? SubmittedAt,
    DateTime? ApprovedAt, DateTime? PaidAt, string? FailureReason
);

// BudgetReservation DTOs
public record BudgetReservationDto(
    int Id, int RefillRequestId, int OfficeId, decimal Amount,
    string Currency, string BudgetMonth, string Status
);

// Notification DTOs
public record NotificationDto(
    int Id, int? RecipientEmployeeId, string? RecipientName,
    int? RefillRequestId, string Type, string Message, bool IsRead, DateTime SentAt
);

// Report DTOs
public record MonthlyReportDto(
    int Year, int Month,
    OfficeReportDto Oslo,
    OfficeReportDto Bengaluru,
    decimal DemoExchangeRateNokToInr,
    decimal OsloSpendingInInr,
    decimal CombinedTotalInr,
    string ExchangeRateLabel
);

public record OfficeReportDto(
    int OfficeId, string OfficeName, string Currency,
    decimal MonthlyBudget, decimal ActualSpending, decimal ActiveReservations,
    decimal AvailableBudget, int CompletedRefills,
    List<CategorySpendingDto> SpendingByCategory,
    List<ReimbursementStatusCountDto> ReimbursementBreakdown
);

public record CategorySpendingDto(string Category, decimal TotalSpending, int RefillCount);

public record ReimbursementStatusCountDto(string Status, int Count, decimal TotalAmount);

public record SnackConsumptionDto(
    int SnackBoxId, string SnackBoxName, string Category,
    int OfficeId, string OfficeName, int RefillCount,
    DateTime? LastRefilledAt
);

// HR Demo DTOs
public record HrClaimDetailDto(
    string ExternalClaimId, int LocalClaimId, string EmployeeName,
    decimal Amount, string Currency, string Status,
    DateTime SubmittedAt, DateTime? ApprovedAt, DateTime? PaidAt, string? RejectionReason
);

public record HrStatusUpdateRequest(string? RejectionReason);

// Error response
public record ApiError(string Code, string Message, Dictionary<string, string[]>? ValidationErrors = null);
