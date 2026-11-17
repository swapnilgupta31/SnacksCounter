using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SnackCounter.Core.DTOs;
using SnackCounter.Core.Enums;
using SnackCounter.Infrastructure;
using SnackCounter.Infrastructure.Data;

namespace SnackCounter.Api.Controllers;

[ApiController]
[Route("api/refills")]
public class RefillsController(SnackCounterDbContext db, RefillService refillService, ReimbursementService reimbursementService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<RefillRequestDto>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] int? officeId)
    {
        var query = db.RefillRequests
            .Include(r => r.SnackBox)
            .Include(r => r.Office)
            .Include(r => r.AssignedVolunteer)
            .Include(r => r.ReportedBy)
            .Include(r => r.Purchase).ThenInclude(p => p!.Employee)
            .Include(r => r.ReimbursementClaim).ThenInclude(c => c!.Employee)
            .AsQueryable();

        if (officeId.HasValue) query = query.Where(r => r.OfficeId == officeId.Value);
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<RefillStatus>(status, true, out var s))
            query = query.Where(r => r.Status == s);

        var refills = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        return Ok(refills.Select(SnackBoxesController.MapRefill));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(RefillRequestDto), 200)]
    public async Task<IActionResult> GetById(int id)
    {
        var r = await db.RefillRequests
            .Include(x => x.SnackBox)
            .Include(x => x.Office)
            .Include(x => x.AssignedVolunteer)
            .Include(x => x.ReportedBy)
            .Include(x => x.Purchase).ThenInclude(p => p!.Employee)
            .Include(x => x.ReimbursementClaim).ThenInclude(c => c!.Employee)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();
        return Ok(SnackBoxesController.MapRefill(r));
    }

    [HttpPost("{id}/accept")]
    [ProducesResponseType(typeof(RefillRequestDto), 200)]
    public async Task<IActionResult> Accept(int id, [FromHeader(Name = "X-Demo-User-Id")] int userId = 1)
    {
        var refill = await refillService.AcceptOfferAsync(id, userId);
        return Ok(SnackBoxesController.MapRefill(refill));
    }

    [HttpPost("{id}/decline")]
    [ProducesResponseType(typeof(RefillRequestDto), 200)]
    public async Task<IActionResult> Decline(int id, [FromHeader(Name = "X-Demo-User-Id")] int userId = 1)
    {
        var refill = await refillService.DeclineOfferAsync(id, userId);
        return Ok(SnackBoxesController.MapRefill(refill));
    }

    [HttpPost("{id}/purchase")]
    [ProducesResponseType(typeof(RefillRequestDto), 200)]
    public async Task<IActionResult> SubmitPurchase(int id, [FromBody] SubmitPurchaseRequest request, [FromHeader(Name = "X-Demo-User-Id")] int userId = 1)
    {
        var refill = await refillService.SubmitPurchaseAsync(id, userId, request.Amount, request.ReceiptReference, request.Description);
        return Ok(SnackBoxesController.MapRefill(refill));
    }

    [HttpPost("{id}/confirm-refilled")]
    [ProducesResponseType(typeof(RefillRequestDto), 200)]
    public async Task<IActionResult> ConfirmRefilled(int id, [FromHeader(Name = "X-Demo-User-Id")] int userId = 1)
    {
        var refill = await refillService.ConfirmRefilledAsync(id, userId);
        return Ok(SnackBoxesController.MapRefill(refill));
    }

    [HttpPost("{id}/cancel")]
    [ProducesResponseType(typeof(RefillRequestDto), 200)]
    public async Task<IActionResult> Cancel(int id, [FromBody] CancelRefillRequest request)
    {
        var refill = await refillService.CancelAsync(id, request.Reason);
        return Ok(SnackBoxesController.MapRefill(refill));
    }

    [HttpPost("{id}/submit-claim")]
    [ProducesResponseType(typeof(ReimbursementClaimDto), 200)]
    public async Task<IActionResult> SubmitClaim(int id)
    {
        var claim = await reimbursementService.CreateClaimAsync(id);
        var submitted = await reimbursementService.SubmitClaimAsync(claim.Id);
        return Ok(MapClaim(submitted));
    }

    [HttpPost("process-expired")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> ProcessExpired()
    {
        var count = await refillService.ProcessExpiredOffersAsync();
        return Ok(new { processed = count });
    }

    private static ReimbursementClaimDto MapClaim(Core.Entities.ReimbursementClaim c) =>
        new(c.Id, c.RefillRequestId, c.EmployeeId, c.Employee?.Name ?? "",
            c.ExternalClaimId, c.Amount, c.Currency, c.Status.ToString(),
            c.CreatedAt, c.SubmittedAt, c.ApprovedAt, c.PaidAt, c.FailureReason);
}
