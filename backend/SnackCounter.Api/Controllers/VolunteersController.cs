using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SnackCounter.Core.DTOs;
using SnackCounter.Core.Enums;
using SnackCounter.Infrastructure;
using SnackCounter.Infrastructure.Data;

namespace SnackCounter.Api.Controllers;

[ApiController]
[Route("api/volunteers")]
public class VolunteersController(SnackCounterDbContext db, VolunteerService volunteerService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<EmployeeDto>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] int? officeId)
    {
        var query = db.Employees
            .Include(e => e.HomeOffice)
            .Include(e => e.CurrentOffice)
            .Where(e => e.IsVolunteer);
        if (officeId.HasValue) query = query.Where(e => e.CurrentOfficeId == officeId.Value);
        var employees = await query.ToListAsync();
        return Ok(employees.Select(MapEmployee));
    }

    [HttpGet("eligible")]
    [ProducesResponseType(typeof(List<EmployeeDto>), 200)]
    public async Task<IActionResult> GetEligible([FromQuery] int officeId)
    {
        var volunteer = await volunteerService.SelectEligibleVolunteerAsync(officeId);
        if (volunteer == null) return Ok(new List<EmployeeDto>());
        await db.Entry(volunteer).Reference(e => e.HomeOffice).LoadAsync();
        await db.Entry(volunteer).Reference(e => e.CurrentOffice).LoadAsync();
        return Ok(new List<EmployeeDto> { MapEmployee(volunteer) });
    }

    [HttpGet("{id}/assignments")]
    [ProducesResponseType(typeof(List<RefillRequestDto>), 200)]
    public async Task<IActionResult> GetAssignments(int id)
    {
        var refills = await db.RefillRequests
            .Include(r => r.SnackBox)
            .Include(r => r.Office)
            .Include(r => r.AssignedVolunteer)
            .Include(r => r.ReportedBy)
            .Include(r => r.Purchase).ThenInclude(p => p!.Employee)
            .Include(r => r.ReimbursementClaim).ThenInclude(c => c!.Employee)
            .Where(r => r.AssignedVolunteerId == id)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        return Ok(refills.Select(SnackBoxesController.MapRefill));
    }

    [HttpPatch("{id}/availability")]
    [ProducesResponseType(typeof(EmployeeDto), 200)]
    public async Task<IActionResult> UpdateAvailability(int id, [FromBody] UpdateAvailabilityRequest request)
    {
        var employee = await db.Employees
            .Include(e => e.HomeOffice)
            .Include(e => e.CurrentOffice)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (employee == null) return NotFound();

        employee.IsAvailable = request.IsAvailable;
        employee.IsOnLeave = request.IsOnLeave;
        employee.IsPresentToday = request.IsPresentToday;
        await db.SaveChangesAsync();
        return Ok(MapEmployee(employee));
    }

    [HttpGet("employees")]
    [ProducesResponseType(typeof(List<EmployeeDto>), 200)]
    public async Task<IActionResult> GetAllEmployees()
    {
        var employees = await db.Employees
            .Include(e => e.HomeOffice)
            .Include(e => e.CurrentOffice)
            .ToListAsync();
        return Ok(employees.Select(MapEmployee));
    }

    private static EmployeeDto MapEmployee(Core.Entities.Employee e) =>
        new(e.Id, e.Name, e.Email, e.HomeOfficeId, e.HomeOffice?.Name ?? "",
            e.CurrentOfficeId, e.CurrentOffice?.Name ?? "",
            e.PayrollCurrency, e.IsVolunteer, e.IsAvailable, e.IsOnLeave, e.IsPresentToday, e.Role);
}
