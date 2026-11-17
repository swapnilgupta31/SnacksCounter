using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SnackCounter.Core.DTOs;
using SnackCounter.Infrastructure;
using SnackCounter.Infrastructure.Data;

namespace SnackCounter.Api.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController(ReportingService reportingService) : ControllerBase
{
    [HttpGet("monthly")]
    [ProducesResponseType(typeof(MonthlyReportDto), 200)]
    public async Task<IActionResult> GetMonthly([FromQuery] int? year, [FromQuery] int? month)
    {
        var now = DateTime.UtcNow;
        var report = await reportingService.GetMonthlyReportAsync(year ?? now.Year, month ?? now.Month);
        return Ok(report);
    }

    [HttpGet("office/{officeId}")]
    [ProducesResponseType(typeof(OfficeReportDto), 200)]
    public async Task<IActionResult> GetOfficeReport(int officeId, [FromQuery] int? year, [FromQuery] int? month)
    {
        var now = DateTime.UtcNow;
        var report = await reportingService.GetMonthlyReportAsync(year ?? now.Year, month ?? now.Month);
        var officeReport = officeId == report.Oslo.OfficeId ? report.Oslo : report.Bengaluru;
        return Ok(officeReport);
    }

    [HttpGet("snack-consumption")]
    [ProducesResponseType(typeof(List<SnackConsumptionDto>), 200)]
    public async Task<IActionResult> GetSnackConsumption()
    {
        var data = await reportingService.GetSnackConsumptionAsync();
        return Ok(data);
    }
}

[ApiController]
[Route("api/notifications")]
public class NotificationsController(SnackCounterDbContext db) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<NotificationDto>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var notifications = await db.Notifications
            .Include(n => n.RecipientEmployee)
            .OrderByDescending(n => n.SentAt)
            .Take(100)
            .ToListAsync();
        return Ok(notifications.Select(MapNotification));
    }

    [HttpGet("employee/{employeeId}")]
    [ProducesResponseType(typeof(List<NotificationDto>), 200)]
    public async Task<IActionResult> GetForEmployee(int employeeId)
    {
        var notifications = await db.Notifications
            .Include(n => n.RecipientEmployee)
            .Where(n => n.RecipientEmployeeId == employeeId || n.RecipientEmployeeId == null)
            .OrderByDescending(n => n.SentAt)
            .Take(50)
            .ToListAsync();
        return Ok(notifications.Select(MapNotification));
    }

    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var n = await db.Notifications.FindAsync(id);
        if (n == null) return NotFound();
        n.IsRead = true;
        await db.SaveChangesAsync();
        return Ok();
    }

    private static NotificationDto MapNotification(Core.Entities.Notification n) =>
        new(n.Id, n.RecipientEmployeeId, n.RecipientEmployee?.Name,
            n.RefillRequestId, n.Type, n.Message, n.IsRead, n.SentAt);
}
