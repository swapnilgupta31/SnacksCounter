using SnackCounter.Core.Interfaces;

namespace SnackCounter.Infrastructure.Integrations;

// In-memory store for the fake HR system — shared as singleton
public class FakeHrStore
{
    private readonly Dictionary<string, FakeHrClaim> _claims = new();
    private readonly object _lock = new();
    private int _nextId = 1000;

    public FakeHrClaim? GetByIdempotencyKey(string key)
    {
        lock (_lock) { return _claims.Values.FirstOrDefault(c => c.IdempotencyKey == key); }
    }

    public FakeHrClaim? GetByExternalId(string externalId)
    {
        lock (_lock) { return _claims.GetValueOrDefault(externalId); }
    }

    public IReadOnlyList<FakeHrClaim> GetAll()
    {
        lock (_lock) { return _claims.Values.ToList(); }
    }

    public FakeHrClaim Create(HrClaimRequest request)
    {
        lock (_lock)
        {
            var existing = _claims.Values.FirstOrDefault(c => c.IdempotencyKey == request.IdempotencyKey);
            if (existing != null) return existing;

            var claim = new FakeHrClaim
            {
                ExternalClaimId = $"HR-EXT-{_nextId++}",
                EmployeeId = request.EmployeeId,
                EmployeeName = request.EmployeeName,
                Amount = request.Amount,
                Currency = request.Currency,
                Description = request.Description,
                ReceiptReference = request.ReceiptReference,
                IdempotencyKey = request.IdempotencyKey,
                Status = "Submitted",
                SubmittedAt = DateTime.UtcNow
            };
            _claims[claim.ExternalClaimId] = claim;
            return claim;
        }
    }

    public bool UpdateStatus(string externalId, string status, string? rejectionReason = null)
    {
        lock (_lock)
        {
            if (!_claims.TryGetValue(externalId, out var claim)) return false;
            claim.Status = status;
            if (status == "Approved") claim.ApprovedAt = DateTime.UtcNow;
            if (status == "Paid") claim.PaidAt = DateTime.UtcNow;
            if (status == "Rejected") { claim.RejectedAt = DateTime.UtcNow; claim.RejectionReason = rejectionReason; }
            return true;
        }
    }
}

public class FakeHrClaim
{
    public string ExternalClaimId { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ReceiptReference { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string Status { get; set; } = "Submitted";
    public DateTime SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }
}

public class FakeHRReimbursementGateway(FakeHrStore store) : IHRReimbursementGateway
{
    // Simulate ~5% failure rate for demo purposes — disabled by default for predictable tests
    public bool SimulateFailure { get; set; } = false;

    public Task<HrClaimResponse> SubmitClaimAsync(HrClaimRequest request)
    {
        if (SimulateFailure)
            return Task.FromResult(new HrClaimResponse(string.Empty, false, "Simulated HR system unavailable"));

        var claim = store.Create(request);
        return Task.FromResult(new HrClaimResponse(claim.ExternalClaimId, true, null));
    }
}
