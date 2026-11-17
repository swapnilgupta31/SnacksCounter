using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using SnackCounter.Core.Entities;
using SnackCounter.Core.Enums;
using SnackCounter.Core.Exceptions;
using SnackCounter.Core.Interfaces;
using SnackCounter.Infrastructure;
using SnackCounter.Infrastructure.Data;
using SnackCounter.Infrastructure.Integrations;
using SnackCounter.Tests.TestHelpers;

namespace SnackCounter.Tests.Unit;

[TestFixture]
public class CurrencyTests
{
    private SnackCounterDbContext _db = null!;
    private TestClock _clock = null!;
    private BudgetService _budgetService = null!;
    private Mock<IChatNotificationService> _chat = null!;
    private RefillService _refillService = null!;
    private VolunteerService _volunteerService = null!;
    private ReportingService _reportingService = null!;

    [SetUp]
    public void SetUp()
    {
        _db = TestDbFactory.Create();
        _clock = new TestClock(DateTime.UtcNow);
        _chat = new Mock<IChatNotificationService>();
        _chat.Setup(c => c.NotifyAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>()))
             .Returns(Task.CompletedTask);
        _budgetService = new BudgetService(_db, _clock);
        _volunteerService = new VolunteerService(_db, _clock);
        _refillService = new RefillService(_db, _budgetService, _volunteerService, _chat.Object, _clock);
        _reportingService = new ReportingService(_db, _budgetService);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    // Test 31: NOK and INR are not directly added
    [Test]
    public async Task NokAndInr_NotDirectlyAdded()
    {
        var (oslo, bengaluru) = await TestData.SeedOfficesAsync(_db);
        var report = await _reportingService.GetMonthlyReportAsync(_clock.UtcNow.Year, _clock.UtcNow.Month);

        Assert.That(report.Oslo.Currency, Is.EqualTo("NOK"));
        Assert.That(report.Bengaluru.Currency, Is.EqualTo("INR"));
        // Combined total uses conversion, not direct addition
        Assert.That(report.DemoExchangeRateNokToInr, Is.EqualTo(9m));
    }

    // Test 32: Consolidated INR report uses configured conversion rate
    [Test]
    public async Task ConsolidatedReport_UsesConfiguredRate()
    {
        var (oslo, bengaluru) = await TestData.SeedOfficesAsync(_db);
        var reporter = await TestData.SeedEmployeeAsync(_db, oslo.Id);
        var vol = await TestData.SeedVolunteerAsync(_db, oslo.Id);
        var box = await TestData.SeedSnackBoxAsync(_db, oslo.Id);

        // Create a completed Oslo purchase of 1000 NOK
        var refill = new RefillRequest
        {
            SnackBoxId = box.Id, OfficeId = oslo.Id, Status = RefillStatus.Refilled,
            AssignedVolunteerId = vol.Id, EstimatedCost = 1000,
            ReportedByEmployeeId = reporter.Id, CreatedAt = _clock.UtcNow,
            CompletedAt = _clock.UtcNow
        };
        _db.RefillRequests.Add(refill);
        await _db.SaveChangesAsync();

        var purchase = new Purchase
        {
            RefillRequestId = refill.Id, EmployeeId = vol.Id,
            Amount = 1000, Currency = "NOK", PurchasedAt = _clock.UtcNow,
            Description = "Test"
        };
        _db.Purchases.Add(purchase);
        await _db.SaveChangesAsync();

        var report = await _reportingService.GetMonthlyReportAsync(_clock.UtcNow.Year, _clock.UtcNow.Month);

        // 1000 NOK * 9 = 9000 INR
        Assert.That(report.OsloSpendingInInr, Is.EqualTo(9000m));
        Assert.That(report.CombinedTotalInr, Is.EqualTo(9000m));
    }

    // Test 33: Original currency values remain unchanged
    [Test]
    public async Task OriginalCurrencyValues_Unchanged()
    {
        var (oslo, _) = await TestData.SeedOfficesAsync(_db);
        var reporter = await TestData.SeedEmployeeAsync(_db, oslo.Id);
        var vol = await TestData.SeedVolunteerAsync(_db, oslo.Id);
        var box = await TestData.SeedSnackBoxAsync(_db, oslo.Id);

        var refill = new RefillRequest
        {
            SnackBoxId = box.Id, OfficeId = oslo.Id, Status = RefillStatus.Refilled,
            AssignedVolunteerId = vol.Id, EstimatedCost = 500,
            ReportedByEmployeeId = reporter.Id, CreatedAt = _clock.UtcNow,
            CompletedAt = _clock.UtcNow
        };
        _db.RefillRequests.Add(refill);
        await _db.SaveChangesAsync();

        _db.Purchases.Add(new Purchase
        {
            RefillRequestId = refill.Id, EmployeeId = vol.Id,
            Amount = 500, Currency = "NOK", PurchasedAt = _clock.UtcNow, Description = "Test"
        });
        await _db.SaveChangesAsync();

        var report = await _reportingService.GetMonthlyReportAsync(_clock.UtcNow.Year, _clock.UtcNow.Month);
        Assert.That(report.Oslo.ActualSpending, Is.EqualTo(500m));
        Assert.That(report.Oslo.Currency, Is.EqualTo("NOK"));
    }

    // Test 34: Visiting employee purchases charged to visited office budget
    [Test]
    public async Task VisitingEmployee_ChargedToVisitedOfficeBudget()
    {
        var (oslo, bengaluru) = await TestData.SeedOfficesAsync(_db);
        var reporter = await TestData.SeedEmployeeAsync(_db, oslo.Id);

        // Oslo employee visiting Bengaluru
        var visitor = new Employee
        {
            Name = "Visitor", Email = "visitor@test.com",
            HomeOfficeId = oslo.Id, CurrentOfficeId = bengaluru.Id,
            PayrollCurrency = "NOK", IsVolunteer = true,
            IsAvailable = true, IsOnLeave = false, IsPresentToday = true, Role = "Volunteer"
        };
        _db.Employees.Add(visitor);
        await _db.SaveChangesAsync();

        var box = await TestData.SeedSnackBoxAsync(_db, bengaluru.Id);
        var refill = await _refillService.ReportEmptyAsync(box.Id, reporter.Id, 800, null);
        await _refillService.AcceptOfferAsync(refill.Id, visitor.Id);

        var bengaluruBudget = await _budgetService.GetBudgetStatusAsync(bengaluru.Id, _clock.UtcNow.Year, _clock.UtcNow.Month);
        var osloBudget = await _budgetService.GetBudgetStatusAsync(oslo.Id, _clock.UtcNow.Year, _clock.UtcNow.Month);

        Assert.That(bengaluruBudget.ActiveReservations, Is.EqualTo(800));
        Assert.That(osloBudget.ActiveReservations, Is.EqualTo(0));
    }
}

[TestFixture]
public class HrTests
{
    private SnackCounterDbContext _db = null!;
    private TestClock _clock = null!;
    private Mock<IChatNotificationService> _chat = null!;
    private FakeHrStore _hrStore = null!;
    private ReimbursementService _reimbursementService = null!;
    private BudgetService _budgetService = null!;
    private VolunteerService _volunteerService = null!;
    private RefillService _refillService = null!;

    [SetUp]
    public void SetUp()
    {
        _db = TestDbFactory.Create();
        _clock = new TestClock(DateTime.UtcNow);
        _chat = new Mock<IChatNotificationService>();
        _chat.Setup(c => c.NotifyAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>()))
             .Returns(Task.CompletedTask);
        _hrStore = new FakeHrStore();
        var hrGateway = new FakeHRReimbursementGateway(_hrStore);
        _reimbursementService = new ReimbursementService(_db, hrGateway, _chat.Object, _clock);
        _budgetService = new BudgetService(_db, _clock);
        _volunteerService = new VolunteerService(_db, _clock);
        _refillService = new RefillService(_db, _budgetService, _volunteerService, _chat.Object, _clock);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private async Task<(RefillRequest refill, int volId)> CreateRefilledRefillAsync()
    {
        var (oslo, _) = await TestData.SeedOfficesAsync(_db);
        var reporter = await TestData.SeedEmployeeAsync(_db, oslo.Id);
        var vol = await TestData.SeedVolunteerAsync(_db, oslo.Id);
        var box = await TestData.SeedSnackBoxAsync(_db, oslo.Id);

        var refill = await _refillService.ReportEmptyAsync(box.Id, reporter.Id, 400, null);
        await _refillService.AcceptOfferAsync(refill.Id, vol.Id);
        await _refillService.SubmitPurchaseAsync(refill.Id, vol.Id, 400, "RCPT-001", "Test purchase");
        await _refillService.ConfirmRefilledAsync(refill.Id, vol.Id);
        return (refill, vol.Id);
    }

    // Test 36: Submitted claim includes required fields
    [Test]
    public async Task SubmittedClaim_HasRequiredFields()
    {
        var (refill, _) = await CreateRefilledRefillAsync();
        var claim = await _reimbursementService.CreateClaimAsync(refill.Id);
        var submitted = await _reimbursementService.SubmitClaimAsync(claim.Id);

        Assert.That(submitted.ExternalClaimId, Is.Not.Null.And.Not.Empty);
        Assert.That(submitted.Amount, Is.EqualTo(400));
        Assert.That(submitted.Currency, Is.EqualTo("NOK"));
        Assert.That(submitted.Status, Is.EqualTo(ClaimStatus.Submitted));
        Assert.That(submitted.IdempotencyKey, Is.Not.Null.And.Not.Empty);
    }

    // Test 37: Claim approval does not imply payment
    [Test]
    public async Task ClaimApproval_DoesNotImplyPayment()
    {
        var (refill, _) = await CreateRefilledRefillAsync();
        var claim = await _reimbursementService.CreateClaimAsync(refill.Id);
        await _reimbursementService.SubmitClaimAsync(claim.Id);
        await _reimbursementService.UpdateClaimStatusAsync(claim.Id, ClaimStatus.Approved);

        var updated = await _db.ReimbursementClaims.FindAsync(claim.Id);
        Assert.That(updated!.Status, Is.EqualTo(ClaimStatus.Approved));
        Assert.That(updated.PaidAt, Is.Null);

        var refillUpdated = await _db.RefillRequests.FindAsync(refill.Id);
        Assert.That(refillUpdated!.Status, Is.Not.EqualTo(RefillStatus.Reimbursed));
    }

    // Test 38: Paid status updates reimbursement correctly
    [Test]
    public async Task PaidStatus_UpdatesRefillToReimbursed()
    {
        var (refill, _) = await CreateRefilledRefillAsync();
        var claim = await _reimbursementService.CreateClaimAsync(refill.Id);
        await _reimbursementService.SubmitClaimAsync(claim.Id);
        await _reimbursementService.UpdateClaimStatusAsync(claim.Id, ClaimStatus.Approved);
        await _reimbursementService.UpdateClaimStatusAsync(claim.Id, ClaimStatus.Paid);

        var updated = await _db.ReimbursementClaims.FindAsync(claim.Id);
        Assert.That(updated!.Status, Is.EqualTo(ClaimStatus.Paid));
        Assert.That(updated.PaidAt, Is.Not.Null);

        var refillUpdated = await _db.RefillRequests.FindAsync(refill.Id);
        Assert.That(refillUpdated!.Status, Is.EqualTo(RefillStatus.Reimbursed));
    }

    // Test 39: Rejected claim remains visible and actionable
    [Test]
    public async Task RejectedClaim_RemainsVisible()
    {
        var (refill, _) = await CreateRefilledRefillAsync();
        var claim = await _reimbursementService.CreateClaimAsync(refill.Id);
        await _reimbursementService.SubmitClaimAsync(claim.Id);
        await _reimbursementService.UpdateClaimStatusAsync(claim.Id, ClaimStatus.Rejected, "Missing receipt");

        var updated = await _db.ReimbursementClaims.FindAsync(claim.Id);
        Assert.That(updated!.Status, Is.EqualTo(ClaimStatus.Rejected));
        Assert.That(updated.FailureReason, Is.EqualTo("Missing receipt"));
    }

    // Test 40: Retried submission does not duplicate the claim
    [Test]
    public async Task RetrySubmission_DoesNotDuplicateClaim()
    {
        var (refill, _) = await CreateRefilledRefillAsync();
        var claim = await _reimbursementService.CreateClaimAsync(refill.Id);
        var first = await _reimbursementService.SubmitClaimAsync(claim.Id);
        var second = await _reimbursementService.SubmitClaimAsync(claim.Id);

        Assert.That(first.ExternalClaimId, Is.EqualTo(second.ExternalClaimId));
        var hrClaims = _hrStore.GetAll().Where(c => c.IdempotencyKey == claim.IdempotencyKey).ToList();
        Assert.That(hrClaims.Count, Is.EqualTo(1));
    }

    // Test 41: HR submission failure preserves refill and purchase data
    [Test]
    public async Task HrFailure_PreservesRefillAndPurchase()
    {
        var (refill, _) = await CreateRefilledRefillAsync();
        var claim = await _reimbursementService.CreateClaimAsync(refill.Id);

        var failingGateway = new FakeHRReimbursementGateway(_hrStore) { SimulateFailure = true };
        var failingSvc = new ReimbursementService(_db, failingGateway, _chat.Object, _clock);

        var result = await failingSvc.SubmitClaimAsync(claim.Id);
        Assert.That(result.Status, Is.EqualTo(ClaimStatus.SubmissionFailed));

        var purchase = await _db.Purchases.FirstOrDefaultAsync(p => p.RefillRequestId == refill.Id);
        Assert.That(purchase, Is.Not.Null);
        Assert.That(purchase!.Amount, Is.EqualTo(400));
    }
}
