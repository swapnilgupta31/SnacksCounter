using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SnackCounter.Core.DTOs;
using SnackCounter.Core.Enums;
using SnackCounter.Infrastructure;
using SnackCounter.Infrastructure.Data;

namespace SnackCounter.Api.Controllers;

[ApiController]
[Route("api/snack-boxes")]
public class SnackBoxesController(SnackCounterDbContext db, RefillService refillService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<SnackBoxDto>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] int? officeId)
    {
        var query = db.SnackBoxes.Include(s => s.Office).Include(s => s.RefillRequests).AsQueryable();
        if (officeId.HasValue) query = query.Where(s => s.OfficeId == officeId.Value);

        var boxes = await query.ToListAsync();
        var activeStatuses = new[] { RefillStatus.Requested, RefillStatus.Offered, RefillStatus.Assigned, RefillStatus.Purchased, RefillStatus.Refilled, RefillStatus.ClaimSubmitted };

        return Ok(boxes.Select(s => new SnackBoxDto(
            s.Id, s.OfficeId, s.Office.Name, s.Name, s.Category,
            s.AvailabilityStatus.ToString(), s.LastRefilledAt,
            s.RefillRequests.FirstOrDefault(r => activeStatuses.Contains(r.Status))?.Id
        )));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SnackBoxDto), 200)]
    public async Task<IActionResult> GetById(int id)
    {
        var s = await db.SnackBoxes.Include(x => x.Office).Include(x => x.RefillRequests).FirstOrDefaultAsync(x => x.Id == id);
        if (s == null) return NotFound();
        var activeStatuses = new[] { RefillStatus.Requested, RefillStatus.Offered, RefillStatus.Assigned, RefillStatus.Purchased, RefillStatus.Refilled, RefillStatus.ClaimSubmitted };
        return Ok(new SnackBoxDto(s.Id, s.OfficeId, s.Office.Name, s.Name, s.Category, s.AvailabilityStatus.ToString(), s.LastRefilledAt, s.RefillRequests.FirstOrDefault(r => activeStatuses.Contains(r.Status))?.Id));
    }

    [HttpPost("{id}/report-empty")]
    [ProducesResponseType(typeof(RefillRequestDto), 201)]
    [ProducesResponseType(typeof(ApiError), 409)]
    public async Task<IActionResult> ReportEmpty(int id, [FromBody] ReportEmptyRequest request, [FromHeader(Name = "X-Demo-User-Id")] int userId = 1)
    {
        var refill = await refillService.ReportEmptyAsync(id, userId, request.EstimatedCost, request.Notes);
        var dto = await MapRefillAsync(refill.Id, db);
        return CreatedAtAction(nameof(GetById), new { id = refill.Id }, dto);
    }

    private static async Task<RefillRequestDto> MapRefillAsync(int refillId, SnackCounterDbContext db)
    {
        var r = await db.RefillRequests
            .Include(x => x.SnackBox)
            .Include(x => x.Office)
            .Include(x => x.AssignedVolunteer)
            .Include(x => x.ReportedBy)
            .Include(x => x.Purchase).ThenInclude(p => p!.Employee)
            .Include(x => x.ReimbursementClaim).ThenInclude(c => c!.Employee)
            .FirstAsync(x => x.Id == refillId);

        return MapRefill(r);
    }

    internal static RefillRequestDto MapRefill(Core.Entities.RefillRequest r)
    {
        PurchaseDto? purchaseDto = r.Purchase == null ? null : new PurchaseDto(
            r.Purchase.Id, r.Purchase.RefillRequestId, r.Purchase.EmployeeId, r.Purchase.Employee?.Name ?? "",
            r.Purchase.Amount, r.Purchase.Currency, r.Purchase.PurchasedAt,
            r.Purchase.ReceiptReference, r.Purchase.Description, r.Purchase.IsOverBudget);

        ReimbursementClaimDto? claimDto = r.ReimbursementClaim == null ? null : new ReimbursementClaimDto(
            r.ReimbursementClaim.Id, r.ReimbursementClaim.RefillRequestId,
            r.ReimbursementClaim.EmployeeId, r.ReimbursementClaim.Employee?.Name ?? "",
            r.ReimbursementClaim.ExternalClaimId, r.ReimbursementClaim.Amount, r.ReimbursementClaim.Currency,
            r.ReimbursementClaim.Status.ToString(), r.ReimbursementClaim.CreatedAt,
            r.ReimbursementClaim.SubmittedAt, r.ReimbursementClaim.ApprovedAt, r.ReimbursementClaim.PaidAt,
            r.ReimbursementClaim.FailureReason);

        return new RefillRequestDto(
            r.Id, r.SnackBoxId, r.SnackBox?.Name ?? "", r.SnackBox?.Category ?? "",
            r.OfficeId, r.Office?.Name ?? "", r.Status.ToString(),
            r.AssignedVolunteerId, r.AssignedVolunteer?.Name,
            r.EstimatedCost, r.Office?.Currency ?? "",
            r.ReportedByEmployeeId, r.ReportedBy?.Name ?? "",
            r.CreatedAt, r.OfferExpiresAt, r.AssignedAt, r.AssignmentDeadline, r.CompletedAt,
            r.Notes, r.CancellationReason, purchaseDto, claimDto);
    }
}
