using Microsoft.EntityFrameworkCore;
using SnackCounter.Core.DTOs;
using SnackCounter.Core.Entities;
using SnackCounter.Core.Enums;
using SnackCounter.Core.Exceptions;
using SnackCounter.Core.Interfaces;
using SnackCounter.Infrastructure.Data;

namespace SnackCounter.Infrastructure;

public class BudgetService(SnackCounterDbContext db, IClock clock)
{
    public string GetBudgetMonth(int officeId)
    {
        var office = db.Offices.Find(officeId) ?? throw new NotFoundException($"Office {officeId} not found");
        var tz = TimeZoneInfo.FindSystemTimeZoneById(NormalizeTimeZoneId(office.TimeZoneId));
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(clock.UtcNow, tz);
        return localNow.ToString("yyyy-MM");
    }

    public async Task<OfficeBudgetDto> GetBudgetStatusAsync(int officeId, int year, int month)
    {
        var office = await db.Offices.FindAsync(officeId)
            ?? throw new NotFoundException($"Office {officeId} not found");

        var budgetMonth = $"{year:D4}-{month:D2}";

        var actualSpending = await db.Purchases
            .Where(p => p.RefillRequest.OfficeId == officeId
                && p.RefillRequest.Status != RefillStatus.Cancelled
                && p.PurchasedAt >= GetMonthStart(office.TimeZoneId, year, month)
                && p.PurchasedAt < GetMonthStart(office.TimeZoneId, year, month).AddMonths(1))
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        var activeReservations = await db.BudgetReservations
            .Where(r => r.OfficeId == officeId && r.BudgetMonth == budgetMonth && r.Status == ReservationStatus.Active)
            .SumAsync(r => (decimal?)r.Amount) ?? 0m;

        var available = office.MonthlyBudget - actualSpending - activeReservations;

        return new OfficeBudgetDto(
            officeId, office.Name, office.Currency,
            office.MonthlyBudget, actualSpending, activeReservations,
            Math.Max(0, available), budgetMonth
        );
    }

    public async Task<BudgetReservation> ReserveBudgetAsync(int refillRequestId, int officeId, decimal amount)
    {
        var office = await db.Offices.FindAsync(officeId)
            ?? throw new NotFoundException($"Office {officeId} not found");

        var budgetMonth = GetBudgetMonth(officeId);

        // If already inside a transaction (e.g. called from AcceptOfferAsync), reuse it.
        // Otherwise open our own transaction for standalone calls.
        var ownTransaction = db.Database.CurrentTransaction == null;
        var tx = ownTransaction ? await db.Database.BeginTransactionAsync() : null;
        try
        {
            var actualSpending = await db.Purchases
                .Where(p => p.RefillRequest.OfficeId == officeId
                    && p.RefillRequest.Status != RefillStatus.Cancelled
                    && p.PurchasedAt >= GetMonthStartFromString(office.TimeZoneId, budgetMonth)
                    && p.PurchasedAt < GetMonthStartFromString(office.TimeZoneId, budgetMonth).AddMonths(1))
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            var activeReservations = await db.BudgetReservations
                .Where(r => r.OfficeId == officeId && r.BudgetMonth == budgetMonth && r.Status == ReservationStatus.Active)
                .SumAsync(r => (decimal?)r.Amount) ?? 0m;

            var available = office.MonthlyBudget - actualSpending - activeReservations;

            if (amount > available)
                throw new BudgetExceededException(
                    $"Insufficient budget. Requested: {amount} {office.Currency}, Available: {available} {office.Currency}",
                    amount, available);

            var reservation = new BudgetReservation
            {
                RefillRequestId = refillRequestId,
                OfficeId = officeId,
                Amount = amount,
                Currency = office.Currency,
                BudgetMonth = budgetMonth,
                Status = ReservationStatus.Active,
                CreatedAt = clock.UtcNow
            };
            db.BudgetReservations.Add(reservation);
            await db.SaveChangesAsync();
            if (tx != null) await tx.CommitAsync();
            return reservation;
        }
        catch
        {
            if (tx != null) await tx.RollbackAsync();
            throw;
        }
        finally
        {
            if (tx != null) await tx.DisposeAsync();
        }
    }

    public async Task ReleaseReservationAsync(int refillRequestId)
    {
        var reservation = await db.BudgetReservations
            .FirstOrDefaultAsync(r => r.RefillRequestId == refillRequestId && r.Status == ReservationStatus.Active);
        if (reservation == null) return;
        reservation.Status = ReservationStatus.Released;
        reservation.ReleasedAt = clock.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task ConvertReservationToActualAsync(int refillRequestId, decimal actualAmount)
    {
        var reservation = await db.BudgetReservations
            .FirstOrDefaultAsync(r => r.RefillRequestId == refillRequestId);
        if (reservation == null) return;
        reservation.Amount = actualAmount;
        reservation.Status = ReservationStatus.Converted;
        reservation.ReleasedAt = clock.UtcNow;
        await db.SaveChangesAsync();
    }

    private DateTime GetMonthStart(string timeZoneId, int year, int month)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(NormalizeTimeZoneId(timeZoneId));
        var localStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(localStart, tz);
    }

    private DateTime GetMonthStartFromString(string timeZoneId, string budgetMonth)
    {
        var parts = budgetMonth.Split('-');
        return GetMonthStart(timeZoneId, int.Parse(parts[0]), int.Parse(parts[1]));
    }

    // Windows uses different timezone IDs than IANA; handle both
    private static string NormalizeTimeZoneId(string id) => id switch
    {
        "Europe/Oslo" => OperatingSystem.IsWindows() ? "W. Europe Standard Time" : "Europe/Oslo",
        "Asia/Kolkata" => OperatingSystem.IsWindows() ? "India Standard Time" : "Asia/Kolkata",
        _ => id
    };
}
