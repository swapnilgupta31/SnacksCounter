using Microsoft.EntityFrameworkCore;
using SnackCounter.Core.DTOs;
using SnackCounter.Core.Entities;
using SnackCounter.Core.Enums;
using SnackCounter.Core.Exceptions;
using SnackCounter.Core.Interfaces;
using SnackCounter.Infrastructure.Data;

namespace SnackCounter.Infrastructure;

public class RefillService(
    SnackCounterDbContext db,
    BudgetService budgetService,
    VolunteerService volunteerService,
    IChatNotificationService chat,
    IClock clock)
{
    private static readonly TimeSpan OfferTimeout = TimeSpan.FromHours(2);
    private static readonly TimeSpan AssignmentDeadline = TimeSpan.FromHours(24);

    public async Task<RefillRequest> ReportEmptyAsync(int snackBoxId, int reportedByEmployeeId, decimal estimatedCost, string? notes)
    {
        if (estimatedCost <= 0)
            throw new ValidationException("Estimated cost must be positive.");

        var snackBox = await db.SnackBoxes.Include(s => s.Office).FirstOrDefaultAsync(s => s.Id == snackBoxId)
            ?? throw new NotFoundException($"SnackBox {snackBoxId} not found");

        // Prevent duplicate active refill — DB unique index is the final guard, but check first for a clear error
        var activeStatuses = new[] { RefillStatus.Requested, RefillStatus.Offered, RefillStatus.Assigned, RefillStatus.Purchased, RefillStatus.Refilled, RefillStatus.ClaimSubmitted };
        var existing = await db.RefillRequests
            .FirstOrDefaultAsync(r => r.SnackBoxId == snackBoxId && activeStatuses.Contains(r.Status));
        if (existing != null)
            throw new ConflictException($"An active refill request already exists for snack box {snackBoxId}.");

        var refill = new RefillRequest
        {
            SnackBoxId = snackBoxId,
            OfficeId = snackBox.OfficeId,
            Status = RefillStatus.Requested,
            EstimatedCost = estimatedCost,
            Notes = notes,
            ReportedByEmployeeId = reportedByEmployeeId,
            CreatedAt = clock.UtcNow
        };

        snackBox.AvailabilityStatus = SnackAvailability.RefillPending;
        db.RefillRequests.Add(refill);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE") == true)
        {
            throw new ConflictException($"An active refill request already exists for snack box {snackBoxId}.");
        }

        await chat.NotifyAsync(null, refill.Id, "NewRefillRequest",
            $"Empty snack box reported: {snackBox.Name} at {snackBox.Office.Name}.");

        await OfferToVolunteerAsync(refill);
        return refill;
    }

    public async Task OfferToVolunteerAsync(RefillRequest refill, int? excludeVolunteerId = null)
    {
        var volunteer = await volunteerService.SelectEligibleVolunteerAsync(refill.OfficeId, excludeVolunteerId);

        if (volunteer == null)
        {
            await chat.NotifyAsync(null, refill.Id, "NoVolunteerAvailable",
                $"No eligible volunteer found for refill #{refill.Id}. Management notified.");
            return;
        }

        refill.Status = RefillStatus.Offered;
        refill.AssignedVolunteerId = volunteer.Id;
        refill.OfferedAt = clock.UtcNow;
        refill.OfferExpiresAt = clock.UtcNow + OfferTimeout;
        await db.SaveChangesAsync();

        await chat.NotifyAsync(volunteer.Id, refill.Id, "AssignmentOffer",
            $"You have been offered a snack refill assignment for refill #{refill.Id}. Please accept or decline within 2 hours.");
    }

    public async Task<RefillRequest> AcceptOfferAsync(int refillId, int volunteerId)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var refill = await db.RefillRequests
                .Include(r => r.SnackBox).ThenInclude(s => s.Office)
                .FirstOrDefaultAsync(r => r.Id == refillId)
                ?? throw new NotFoundException($"Refill {refillId} not found");

            if (refill.Status != RefillStatus.Offered)
                throw new InvalidStateTransitionException($"Refill is not in Offered state (current: {refill.Status}).");

            if (refill.AssignedVolunteerId != volunteerId)
                throw new ConflictException("This offer was not made to you.");

            if (refill.OfferExpiresAt < clock.UtcNow)
                throw new InvalidStateTransitionException("This offer has expired.");

            refill.Status = RefillStatus.Assigned;
            refill.AssignedAt = clock.UtcNow;
            refill.AssignmentDeadline = clock.UtcNow + AssignmentDeadline;

            // Reserve budget atomically
            await budgetService.ReserveBudgetAsync(refillId, refill.OfficeId, refill.EstimatedCost);

            var volunteer = await db.Employees.FindAsync(volunteerId);
            if (volunteer != null) volunteer.LastAssignedAt = clock.UtcNow;

            await db.SaveChangesAsync();
            await tx.CommitAsync();

            await chat.NotifyAsync(volunteerId, refillId, "OfferAccepted",
                $"You have accepted refill assignment #{refillId}. Please complete within 24 hours.");

            return refill;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<RefillRequest> DeclineOfferAsync(int refillId, int volunteerId)
    {
        var refill = await db.RefillRequests.FindAsync(refillId)
            ?? throw new NotFoundException($"Refill {refillId} not found");

        if (refill.Status != RefillStatus.Offered)
            throw new InvalidStateTransitionException($"Refill is not in Offered state.");

        if (refill.AssignedVolunteerId != volunteerId)
            throw new ConflictException("This offer was not made to you.");

        refill.Status = RefillStatus.Requested;
        refill.AssignedVolunteerId = null;
        refill.OfferedAt = null;
        refill.OfferExpiresAt = null;
        await db.SaveChangesAsync();

        await chat.NotifyAsync(volunteerId, refillId, "OfferDeclined",
            $"You declined refill assignment #{refillId}. Searching for another volunteer.");

        await OfferToVolunteerAsync(refill, excludeVolunteerId: volunteerId);
        return refill;
    }

    public async Task<RefillRequest> SubmitPurchaseAsync(int refillId, int volunteerId, decimal amount, string? receiptReference, string description)
    {
        if (amount <= 0)
            throw new ValidationException("Purchase amount must be positive.");

        var refill = await db.RefillRequests
            .Include(r => r.SnackBox).ThenInclude(s => s.Office)
            .Include(r => r.BudgetReservation)
            .FirstOrDefaultAsync(r => r.Id == refillId)
            ?? throw new NotFoundException($"Refill {refillId} not found");

        if (refill.Status != RefillStatus.Assigned)
            throw new InvalidStateTransitionException($"Purchase can only be submitted for Assigned refills (current: {refill.Status}).");

        if (refill.AssignedVolunteerId != volunteerId)
            throw new ConflictException("Only the assigned volunteer can submit a purchase.");

        var office = refill.SnackBox.Office;
        bool isOverBudget = false;

        // Check if actual amount exceeds available budget (reservation already held)
        if (refill.BudgetReservation != null)
        {
            var reservedAmount = refill.BudgetReservation.Amount;
            var extraNeeded = amount - reservedAmount;
            if (extraNeeded > 0)
            {
                // Check if extra is affordable
                var budget = await budgetService.GetBudgetStatusAsync(refill.OfficeId,
                    clock.UtcNow.Year, clock.UtcNow.Month);
                if (extraNeeded > budget.AvailableBudget)
                    isOverBudget = true; // Flag but don't reject — real purchase already happened
            }
        }

        var purchase = new Purchase
        {
            RefillRequestId = refillId,
            EmployeeId = volunteerId,
            Amount = amount,
            Currency = office.Currency,
            PurchasedAt = clock.UtcNow,
            ReceiptReference = receiptReference,
            Description = description,
            IsOverBudget = isOverBudget
        };
        db.Purchases.Add(purchase);

        refill.Status = RefillStatus.Purchased;
        await budgetService.ConvertReservationToActualAsync(refillId, amount);
        await db.SaveChangesAsync();

        if (isOverBudget)
            await chat.NotifyAsync(null, refillId, "BudgetWarning",
                $"Refill #{refillId} purchase of {amount} {office.Currency} exceeds available budget. Flagged for review.");

        await chat.NotifyAsync(volunteerId, refillId, "PurchaseCompleted",
            $"Purchase recorded for refill #{refillId}: {amount} {office.Currency}.");

        return refill;
    }

    public async Task<RefillRequest> ConfirmRefilledAsync(int refillId, int volunteerId)
    {
        var refill = await db.RefillRequests
            .Include(r => r.SnackBox)
            .Include(r => r.Purchase)
            .FirstOrDefaultAsync(r => r.Id == refillId)
            ?? throw new NotFoundException($"Refill {refillId} not found");

        if (refill.Status != RefillStatus.Purchased)
            throw new InvalidStateTransitionException($"Refill confirmation requires Purchased status (current: {refill.Status}).");

        if (refill.AssignedVolunteerId != volunteerId)
            throw new ConflictException("Only the assigned volunteer can confirm the refill.");

        if (refill.Purchase == null)
            throw new InvalidStateTransitionException("No purchase recorded for this refill.");

        refill.Status = RefillStatus.Refilled;
        refill.CompletedAt = clock.UtcNow;
        refill.SnackBox.AvailabilityStatus = SnackAvailability.Available;
        refill.SnackBox.LastRefilledAt = clock.UtcNow;
        await db.SaveChangesAsync();

        return refill;
    }

    public async Task<RefillRequest> CancelAsync(int refillId, string reason)
    {
        var refill = await db.RefillRequests.FindAsync(refillId)
            ?? throw new NotFoundException($"Refill {refillId} not found");

        var cancellableStatuses = new[] { RefillStatus.Requested, RefillStatus.Offered, RefillStatus.Assigned };
        if (!cancellableStatuses.Contains(refill.Status))
            throw new InvalidStateTransitionException($"Cannot cancel refill in status {refill.Status}.");

        // Release budget reservation if one exists and no purchase was made
        if (refill.Status == RefillStatus.Assigned)
            await budgetService.ReleaseReservationAsync(refillId);

        refill.Status = RefillStatus.Cancelled;
        refill.CancellationReason = reason;

        var snackBox = await db.SnackBoxes.FindAsync(refill.SnackBoxId);
        if (snackBox != null) snackBox.AvailabilityStatus = SnackAvailability.Empty;

        await db.SaveChangesAsync();
        return refill;
    }

    public async Task<int> ProcessExpiredOffersAsync()
    {
        var expiredOffers = await db.RefillRequests
            .Where(r => r.Status == RefillStatus.Offered && r.OfferExpiresAt < clock.UtcNow)
            .ToListAsync();

        foreach (var refill in expiredOffers)
        {
            var previousVolunteerId = refill.AssignedVolunteerId;
            refill.Status = RefillStatus.Requested;
            refill.AssignedVolunteerId = null;
            refill.OfferedAt = null;
            refill.OfferExpiresAt = null;
            await db.SaveChangesAsync();

            await chat.NotifyAsync(previousVolunteerId, refill.Id, "OfferExpired",
                $"Assignment offer #{refill.Id} expired. Reassigning to another volunteer.");

            await OfferToVolunteerAsync(refill, excludeVolunteerId: previousVolunteerId);
        }

        return expiredOffers.Count;
    }
}
