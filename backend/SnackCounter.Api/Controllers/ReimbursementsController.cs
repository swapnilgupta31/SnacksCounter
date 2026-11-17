using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SnackCounter.Core.DTOs;
using SnackCounter.Core.Enums;
using SnackCounter.Infrastructure;
using SnackCounter.Infrastructure.Data;

namespace SnackCounter.Api.Controllers;

[ApiController]
[Route("api/reimbursements")]
public class ReimbursementsController(SnackCounterDbContext db, ReimbursementService reimbursementService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<ReimbursementClaimDto>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var claims = await db.ReimbursementClaims
            .Include(c => c.Employee)
            .Include(c => c.RefillRequest)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
        return Ok(claims.Select(MapClaim));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ReimbursementClaimDto), 200)]
    public async Task<IActionResult> GetById(int id)
    {
        var c = await db.ReimbursementClaims
            .Include(x => x.Employee)
            .Include(x => x.RefillRequest)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound();
        return Ok(MapClaim(c));
    }

    [HttpPost("{id}/submit")]
    [ProducesResponseType(typeof(ReimbursementClaimDto), 200)]
    public async Task<IActionResult> Submit(int id)
    {
        var claim = await reimbursementService.SubmitClaimAsync(id);
        return Ok(MapClaim(claim));
    }

    [HttpPost("{id}/retry")]
    [ProducesResponseType(typeof(ReimbursementClaimDto), 200)]
    public async Task<IActionResult> Retry(int id)
    {
        var claim = await reimbursementService.SubmitClaimAsync(id);
        return Ok(MapClaim(claim));
    }

    private static ReimbursementClaimDto MapClaim(Core.Entities.ReimbursementClaim c) =>
        new(c.Id, c.RefillRequestId, c.EmployeeId, c.Employee?.Name ?? "",
            c.ExternalClaimId, c.Amount, c.Currency, c.Status.ToString(),
            c.CreatedAt, c.SubmittedAt, c.ApprovedAt, c.PaidAt, c.FailureReason);
}
