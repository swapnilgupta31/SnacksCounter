using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SnackCounter.Core.DTOs;
using SnackCounter.Infrastructure;
using SnackCounter.Infrastructure.Data;

namespace SnackCounter.Api.Controllers;

[ApiController]
[Route("api/offices")]
public class OfficesController(SnackCounterDbContext db, BudgetService budgetService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<OfficeDto>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var offices = await db.Offices.ToListAsync();
        return Ok(offices.Select(o => new OfficeDto(o.Id, o.Name, o.Currency, o.MonthlyBudget, o.TimeZoneId)));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OfficeDto), 200)]
    public async Task<IActionResult> GetById(int id)
    {
        var o = await db.Offices.FindAsync(id);
        if (o == null) return NotFound();
        return Ok(new OfficeDto(o.Id, o.Name, o.Currency, o.MonthlyBudget, o.TimeZoneId));
    }

    [HttpGet("{id}/budget")]
    [ProducesResponseType(typeof(OfficeBudgetDto), 200)]
    public async Task<IActionResult> GetBudget(int id, [FromQuery] int? year, [FromQuery] int? month)
    {
        var now = DateTime.UtcNow;
        var budget = await budgetService.GetBudgetStatusAsync(id, year ?? now.Year, month ?? now.Month);
        return Ok(budget);
    }

    [HttpGet("{id}/snack-boxes")]
    [ProducesResponseType(typeof(List<SnackBoxDto>), 200)]
    public async Task<IActionResult> GetSnackBoxes(int id)
    {
        var boxes = await db.SnackBoxes
            .Include(s => s.Office)
            .Include(s => s.RefillRequests)
            .Where(s => s.OfficeId == id)
            .ToListAsync();

        var activeStatuses = new[] { Core.Enums.RefillStatus.Requested, Core.Enums.RefillStatus.Offered, Core.Enums.RefillStatus.Assigned, Core.Enums.RefillStatus.Purchased, Core.Enums.RefillStatus.Refilled, Core.Enums.RefillStatus.ClaimSubmitted };

        return Ok(boxes.Select(s => new SnackBoxDto(
            s.Id, s.OfficeId, s.Office.Name, s.Name, s.Category,
            s.AvailabilityStatus.ToString(), s.LastRefilledAt,
            s.RefillRequests.FirstOrDefault(r => activeStatuses.Contains(r.Status))?.Id
        )));
    }
}
