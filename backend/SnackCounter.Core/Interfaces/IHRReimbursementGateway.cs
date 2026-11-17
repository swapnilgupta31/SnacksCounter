namespace SnackCounter.Core.Interfaces;

public record HrClaimRequest(
    int EmployeeId,
    string EmployeeName,
    decimal Amount,
    string Currency,
    string Description,
    string ReceiptReference,
    string IdempotencyKey
);

public record HrClaimResponse(
    string ExternalClaimId,
    bool Success,
    string? ErrorMessage
);

public interface IHRReimbursementGateway
{
    Task<HrClaimResponse> SubmitClaimAsync(HrClaimRequest request);
}
