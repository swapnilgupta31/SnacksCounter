using Microsoft.EntityFrameworkCore;
using SnackCounter.Core.DTOs;
using SnackCounter.Core.Enums;
using SnackCounter.Core.Exceptions;
using SnackCounter.Infrastructure.Data;

namespace SnackCounter.Infrastructure;

public class ReportingService(SnackCounterDbContext db, BudgetService budgetService)
{
    private const decimal DemoNokToInrRate = 9m;

    public async Task<MonthlyReportDto> GetMonthlyReportAsync(int year, int month)
    {
        var offices = await db.Offices.ToListAsync();
        var oslo = offices.FirstOrDefault(o => o.Name == "Oslo")
            ?? throw new NotFoundException("Oslo office not found");
        var bengaluru = offices.FirstOrDefault(o => o.Name == "Bengaluru")
            ?? throw new NotFoundException("Bengaluru office not found");

        var osloReport = await BuildOfficeReportAsync(oslo.Id, year, month);
        var bengaluruReport = await BuildOfficeReportAsync(bengaluru.Id, year, month);

        var osloInInr = osloReport.ActualSpending * DemoNokToInrRate;
        var combined = osloInInr + bengaluruReport.ActualSpending;

        return new MonthlyReportDto(
            year, month, osloReport, bengaluruReport,
            DemoNokToInrRate, osloInInr, combined,
            "DEMO_ILLUSTRATIVE_RATE: 1 NOK = 9 INR. Not a live market rate."
        );
    }

    private async Task<OfficeReportDto> BuildOfficeReportAsync(int officeId, int year, int month)
    {
        var budget = await budgetService.GetBudgetStatusAsync(officeId, year, month);
        var office = await db.Offices.FindAsync(officeId);

        var budgetMonth = $"{year:D4}-{month:D2}";

        var completedRefills = await db.RefillRequests
            .Where(r => r.OfficeId == officeId
                && r.CompletedAt != null
                && r.CompletedAt.Value.Year == year
                && r.CompletedAt.Value.Month == month
                && r.Status != RefillStatus.Cancelled)
            .CountAsync();

        var categorySpending = await db.Purchases
            .Include(p => p.RefillRequest).ThenInclude(r => r.SnackBox)
            .Where(p => p.RefillRequest.OfficeId == officeId
                && p.RefillRequest.Status != RefillStatus.Cancelled
                && p.PurchasedAt.Year == year && p.PurchasedAt.Month == month)
            .GroupBy(p => p.RefillRequest.SnackBox.Category)
            .Select(g => new CategorySpendingDto(g.Key, g.Sum(p => p.Amount), g.Count()))
            .ToListAsync();

        var reimbursementBreakdown = await db.ReimbursementClaims
            .Where(c => c.RefillRequest.OfficeId == officeId
                && c.CreatedAt.Year == year && c.CreatedAt.Month == month)
            .GroupBy(c => c.Status)
            .Select(g => new ReimbursementStatusCountDto(g.Key.ToString(), g.Count(), g.Sum(c => c.Amount)))
            .ToListAsync();

        return new OfficeReportDto(
            officeId, office!.Name, office.Currency,
            budget.MonthlyBudget, budget.ActualSpending, budget.ActiveReservations,
            budget.AvailableBudget, completedRefills,
            categorySpending, reimbursementBreakdown
        );
    }

    public async Task<List<SnackConsumptionDto>> GetSnackConsumptionAsync()
    {
        return await db.RefillRequests
            .Include(r => r.SnackBox).ThenInclude(s => s.Office)
            .Where(r => r.Status != RefillStatus.Cancelled && r.CompletedAt != null)
            .GroupBy(r => new { r.SnackBoxId, r.SnackBox.Name, r.SnackBox.Category, r.OfficeId, OfficeName = r.Office.Name })
            .Select(g => new SnackConsumptionDto(
                g.Key.SnackBoxId, g.Key.Name, g.Key.Category,
                g.Key.OfficeId, g.Key.OfficeName, g.Count(),
                g.Max(r => r.CompletedAt)
            ))
            .OrderByDescending(s => s.RefillCount)
            .ToListAsync();
    }
}
