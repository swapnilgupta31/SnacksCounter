using Microsoft.EntityFrameworkCore;
using SnackCounter.Core.DTOs;
using SnackCounter.Core.Entities;
using SnackCounter.Core.Enums;
using SnackCounter.Core.Exceptions;
using SnackCounter.Core.Interfaces;
using SnackCounter.Infrastructure.Data;
using SnackCounter.Infrastructure.Integrations;

namespace SnackCounter.Infrastructure;

public class ReimbursementService(
    SnackCounterDbContext db,
    IHRReimbursementGateway hrGateway,
    IChatNotificationService chat,
    IClock clock)
{
    public async Task<ReimbursementClaim> CreateClaimAsync(int refillId)
    {
        var refill = await db.RefillRequests
            .Include(r => r.Purchase)
            .Include(r => r.AssignedVolunteer)
            .Include(r => r.SnackBox)
            .FirstOrDefaultAsync(r => r.Id == refillId)
            ?? throw new NotFoundException($"Refill {refillId} not found");

        if (refill.Status != RefillStatus.Refilled)
            throw new InvalidStateTransitionException($"Claim requires Refilled status (current: {refill.Status}).");

        if (refill.Purchase == null)
            throw new InvalidStateTransitionException("No purchase recorded.");

        if (string.IsNullOrEmpty(refill.Purchase.ReceiptReference))
            throw new ValidationException("Receipt reference is required before submitting a claim.");

        // Idempotency: return existing claim if already created
        var existing = await db.ReimbursementClaims.FirstOrDefaultAsync(c => c.RefillRequestId == refillId);
        if (existing != null) return existing;

        var claim = new ReimbursementClaim
        {
            RefillRequestId = refillId,
            EmployeeId = refill.AssignedVolunteer!.Id,
            Amount = refill.Purchase.Amount,
            Currency = refill.Purchase.Currency,
            Status = ClaimStatus.PendingSubmission,
            IdempotencyKey = $"refill-{refillId}-v1",
            CreatedAt = clock.UtcNow
        };
        db.ReimbursementClaims.Add(claim);
        await db.SaveChangesAsync();
        return claim;
    }

    public async Task<ReimbursementClaim> SubmitClaimAsync(int claimId)
    {
        var claim = await db.ReimbursementClaims
            .Include(c => c.Employee)
            .Include(c => c.RefillRequest).ThenInclude(r => r.Purchase)
            .FirstOrDefaultAsync(c => c.Id == claimId)
            ?? throw new NotFoundException($"Claim {claimId} not found");

        if (claim.Status == ClaimStatus.Submitted || claim.Status == ClaimStatus.Approved || claim.Status == ClaimStatus.Paid)
            return claim; // Already submitted — idempotent

        if (claim.Status != ClaimStatus.PendingSubmission && claim.Status != ClaimStatus.SubmissionFailed)
            throw new InvalidStateTransitionException($"Cannot submit claim in status {claim.Status}.");

        var request = new HrClaimRequest(
            claim.EmployeeId,
            claim.Employee.Name,
            claim.Amount,
            claim.Currency,
            claim.RefillRequest.Purchase?.Description ?? "Snack refill",
            claim.RefillRequest.Purchase?.ReceiptReference ?? string.Empty,
            claim.IdempotencyKey
        );

        var response = await hrGateway.SubmitClaimAsync(request);

        if (!response.Success)
        {
            claim.Status = ClaimStatus.SubmissionFailed;
            claim.FailureReason = response.ErrorMessage;
            await db.SaveChangesAsync();
            await chat.NotifyAsync(claim.EmployeeId, claim.RefillRequestId, "ClaimSubmissionFailed",
                $"HR claim submission failed: {response.ErrorMessage}. Purchase and refill data preserved.");
            return claim;
        }

        claim.ExternalClaimId = response.ExternalClaimId;
        claim.Status = ClaimStatus.Submitted;
        claim.SubmittedAt = clock.UtcNow;
        claim.FailureReason = null;

        claim.RefillRequest.Status = RefillStatus.ClaimSubmitted;
        await db.SaveChangesAsync();

        await chat.NotifyAsync(claim.EmployeeId, claim.RefillRequestId, "ClaimSubmitted",
            $"HR claim {response.ExternalClaimId} submitted for {claim.Amount} {claim.Currency}.");

        return claim;
    }

    public async Task UpdateClaimStatusAsync(int claimId, ClaimStatus newStatus, string? rejectionReason = null)
    {
        var claim = await db.ReimbursementClaims
            .Include(c => c.RefillRequest)
            .FirstOrDefaultAsync(c => c.Id == claimId)
            ?? throw new NotFoundException($"Claim {claimId} not found");

        claim.Status = newStatus;
        switch (newStatus)
        {
            case ClaimStatus.Approved:
                claim.ApprovedAt = clock.UtcNow;
                await chat.NotifyAsync(claim.EmployeeId, claim.RefillRequestId, "ClaimApproved",
                    $"HR claim {claim.ExternalClaimId} approved.");
                break;
            case ClaimStatus.Rejected:
                claim.RejectedAt = clock.UtcNow;
                claim.FailureReason = rejectionReason;
                await chat.NotifyAsync(claim.EmployeeId, claim.RefillRequestId, "ClaimRejected",
                    $"HR claim {claim.ExternalClaimId} rejected: {rejectionReason}.");
                break;
            case ClaimStatus.Paid:
                claim.PaidAt = clock.UtcNow;
                claim.RefillRequest.Status = RefillStatus.Reimbursed;
                await chat.NotifyAsync(claim.EmployeeId, claim.RefillRequestId, "ClaimPaid",
                    $"HR claim {claim.ExternalClaimId} paid. Reimbursement complete.");
                break;
        }
        await db.SaveChangesAsync();
    }
}
