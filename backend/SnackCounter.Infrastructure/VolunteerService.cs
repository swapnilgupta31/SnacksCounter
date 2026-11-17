using Microsoft.EntityFrameworkCore;
using SnackCounter.Core.Entities;
using SnackCounter.Core.Enums;
using SnackCounter.Core.Interfaces;
using SnackCounter.Infrastructure.Data;

namespace SnackCounter.Infrastructure;

public class VolunteerService(SnackCounterDbContext db, IClock clock)
{
    private static readonly TimeSpan RollingWindow = TimeSpan.FromDays(30);

    public async Task<Employee?> SelectEligibleVolunteerAsync(int officeId, int? excludeEmployeeId = null)
    {
        var windowStart = clock.UtcNow - RollingWindow;

        // Eligible: volunteer, available, not on leave, present today, at the target office
        var candidates = await db.Employees
            .Where(e => e.CurrentOfficeId == officeId
                && e.IsVolunteer
                && e.IsAvailable
                && !e.IsOnLeave
                && e.IsPresentToday
                && (excludeEmployeeId == null || e.Id != excludeEmployeeId))
            .ToListAsync();

        if (!candidates.Any()) return null;

        // Exclude volunteers already committed to an active refill at this office
        var busyVolunteerIds = await db.RefillRequests
            .Where(r => r.OfficeId == officeId
                && r.AssignedVolunteerId != null
                && (r.Status == RefillStatus.Assigned || r.Status == RefillStatus.Purchased))
            .Select(r => r.AssignedVolunteerId!.Value)
            .ToListAsync();

        candidates = candidates.Where(e => !busyVolunteerIds.Contains(e.Id)).ToList();
        if (!candidates.Any()) return null;

        // Count completed refills in rolling 30-day window per candidate
        var candidateIds = candidates.Select(e => e.Id).ToList();
        var recentCounts = await db.RefillRequests
            .Where(r => r.AssignedVolunteerId != null
                && candidateIds.Contains(r.AssignedVolunteerId.Value)
                && r.CompletedAt >= windowStart
                && r.Status != RefillStatus.Cancelled)
            .GroupBy(r => r.AssignedVolunteerId!.Value)
            .Select(g => new { VolunteerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.VolunteerId, x => x.Count);

        // Select: lowest recent count → oldest last-assigned → lowest ID (deterministic)
        return candidates
            .OrderBy(e => recentCounts.GetValueOrDefault(e.Id, 0))
            .ThenBy(e => e.LastAssignedAt ?? DateTime.MinValue)
            .ThenBy(e => e.Id)
            .First();
    }
}
