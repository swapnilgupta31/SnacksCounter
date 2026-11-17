using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SnackCounter.Core.DTOs;
using SnackCounter.Core.Enums;
using SnackCounter.Infrastructure;
using SnackCounter.Infrastructure.Data;
using SnackCounter.Infrastructure.Integrations;

namespace SnackCounter.Api.Controllers;

[ApiController]
[Route("api/demo/hr")]
public class DemoHrController(FakeHrStore hrStore, ReimbursementService reimbursementService, SnackCounterDbContext db) : ControllerBase
{
    [HttpGet("claims")]
    [ProducesResponseType(typeof(List<HrClaimDetailDto>), 200)]
    public IActionResult GetClaims()
    {
        var claims = hrStore.GetAll();
        return Ok(claims.Select(c => new HrClaimDetailDto(
            c.ExternalClaimId, 0, c.EmployeeName, c.Amount, c.Currency,
            c.Status, c.SubmittedAt, c.ApprovedAt, c.PaidAt, c.RejectionReason)));
    }

    [HttpPost("claims/{externalId}/approve")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Approve(string externalId)
    {
        if (!hrStore.UpdateStatus(externalId, "Approved"))
            return NotFound();

        var claim = await db.ReimbursementClaims.FirstOrDefaultAsync(c => c.ExternalClaimId == externalId);
        if (claim != null)
            await reimbursementService.UpdateClaimStatusAsync(claim.Id, ClaimStatus.Approved);

        return Ok(new { message = $"Claim {externalId} approved." });
    }

    [HttpPost("claims/{externalId}/reject")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Reject(string externalId, [FromBody] HrStatusUpdateRequest request)
    {
        if (!hrStore.UpdateStatus(externalId, "Rejected", request.RejectionReason))
            return NotFound();

        var claim = await db.ReimbursementClaims.FirstOrDefaultAsync(c => c.ExternalClaimId == externalId);
        if (claim != null)
            await reimbursementService.UpdateClaimStatusAsync(claim.Id, ClaimStatus.Rejected, request.RejectionReason);

        return Ok(new { message = $"Claim {externalId} rejected." });
    }

    [HttpPost("claims/{externalId}/mark-paid")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> MarkPaid(string externalId)
    {
        if (!hrStore.UpdateStatus(externalId, "Paid"))
            return NotFound();

        var claim = await db.ReimbursementClaims.FirstOrDefaultAsync(c => c.ExternalClaimId == externalId);
        if (claim != null)
            await reimbursementService.UpdateClaimStatusAsync(claim.Id, ClaimStatus.Paid);

        return Ok(new { message = $"Claim {externalId} marked as paid." });
    }
}
