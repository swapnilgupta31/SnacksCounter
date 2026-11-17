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
public class LifecycleTests
{
    private SnackCounterDbContext _db = null!;
    private TestClock _clock = null!;
    private Mock<IChatNotificationService> _chat = null!;
    private RefillService _refillService = null!;
    private BudgetService _budgetService = null!;
    private VolunteerService _volunteerService = null!;
    private ReimbursementService _reimbursementService = null!;

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
        var hrGateway = new FakeHRReimbursementGateway(new FakeHrStore());
        _reimbursementService = new ReimbursementService(_db, hrGateway, _chat.Object, _clock);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    // Test 13: Requested → Assigned succeeds under valid conditions
    [Test]
    public async Task RequestedToAssigned_ValidConditions_Succeeds()
    {
        var (oslo, _) = await TestData.SeedOfficesAsync(_db);
        var reporter = await TestData.SeedEmployeeAsync(_db, oslo.Id);
        var volunteer = await TestData.SeedVolunteerAsync(_db, oslo.Id);
        var box = await TestData.SeedSnackBoxAsync(_db, oslo.Id);

        var refill = await _refillService.ReportEmptyAsync(box.Id, reporter.Id, 400, null);
        // Offer was auto-made; accept it
        var offered = await _db.RefillRequests.FindAsync(refill.Id);
        Assert.That(offered!.Status, Is.EqualTo(RefillStatus.Offered));

        var assigned = await _refillService.AcceptOfferAsync(refill.Id, volunteer.Id);
        Assert.That(assigned.Status, Is.EqualTo(RefillStatus.Assigned));
    }

    // Test 14: Purchased cannot occur before assignment
    [Test]
    public async Task Purchase_BeforeAssignment_Throws()
    {
        var (oslo, _) = await TestData.SeedOfficesAsync(_db);
        var reporter = await TestData.SeedEmployeeAsync(_db, oslo.Id);
        var box = await TestData.SeedSnackBoxAsync(_db, oslo.Id);

        // Create refill in Requested state (no volunteer available)
        var refill = new RefillRequest
        {
            SnackBoxId = box.Id, OfficeId = oslo.Id, Status = RefillStatus.Requested,
            EstimatedCost = 400, ReportedByEmployeeId = reporter.Id, CreatedAt = _clock.UtcNow
        };
        _db.RefillRequests.Add(refill);
        await _db.SaveChangesAsync();

        Assert.ThrowsAsync<InvalidStateTransitionException>(() =>
            _refillService.SubmitPurchaseAsync(refill.Id, reporter.Id, 400, "RCPT-001", "Test"));
    }

    // Test 15: Only assigned volunteer can record purchase
    [Test]
    public async Task Purchase_ByWrongVolunteer_Throws()
    {
        var (oslo, _) = await TestData.SeedOfficesAsync(_db);
        var reporter = await TestData.SeedEmployeeAsync(_db, oslo.Id);
        var volunteer1 = await TestData.SeedVolunteerAsync(_db, oslo.Id, "Vol1");
        var volunteer2 = await TestData.SeedVolunteerAsync(_db, oslo.Id, "Vol2");
        var box = await TestData.SeedSnackBoxAsync(_db, oslo.Id);

        var refill = await _refillService.ReportEmptyAsync(box.Id, reporter.Id, 400, null);
        await _refillService.AcceptOfferAsync(refill.Id, volunteer1.Id);

        Assert.ThrowsAsync<ConflictException>(() =>
            _refillService.SubmitPurchaseAsync(refill.Id, volunteer2.Id, 400, "RCPT-001", "Test"));
    }

    // Test 16: Refill confirmation requires a recorded purchase
    [Test]
    public async Task ConfirmRefill_WithoutPurchase_Throws()
    {
        var (oslo, _) = await TestData.SeedOfficesAsync(_db);
        var reporter = await TestData.SeedEmployeeAsync(_db, oslo.Id);
        var volunteer = await TestData.SeedVolunteerAsync(_db, oslo.Id);
        var box = await TestData.SeedSnackBoxAsync(_db, oslo.Id);

        var refill = await _refillService.ReportEmptyAsync(box.Id, reporter.Id, 400, null);
        await _refillService.AcceptOfferAsync(refill.Id, volunteer.Id);

        Assert.ThrowsAsync<InvalidStateTransitionException>(() =>
            _refillService.ConfirmRefilledAsync(refill.Id, volunteer.Id));
    }

    // Test 17: Reimbursement submission requires completed refill and receipt
    [Test]
    public async Task SubmitClaim_WithoutReceipt_Throws()
    {
        var (oslo, _) = await TestData.SeedOfficesAsync(_db);
        var reporter = await TestData.SeedEmployeeAsync(_db, oslo.Id);
        var volunteer = await TestData.SeedVolunteerAsync(_db, oslo.Id);
        var box = await TestData.SeedSnackBoxAsync(_db, oslo.Id);

        var refill = await _refillService.ReportEmptyAsync(box.Id, reporter.Id, 400, null);
        await _refillService.AcceptOfferAsync(refill.Id, volunteer.Id);
        await _refillService.SubmitPurchaseAsync(refill.Id, volunteer.Id, 400, null, "Test purchase"); // no receipt
        await _refillService.ConfirmRefilledAsync(refill.Id, volunteer.Id);

        Assert.ThrowsAsync<ValidationException>(() =>
            _reimbursementService.CreateClaimAsync(refill.Id));
    }

    // Test 18: Reimbursed cannot occur before HR confirms payment
    [Test]
    public async Task Reimbursed_RequiresHrPayment()
    {
        var (oslo, _) = await TestData.SeedOfficesAsync(_db);
        var reporter = await TestData.SeedEmployeeAsync(_db, oslo.Id);
        var volunteer = await TestData.SeedVolunteerAsync(_db, oslo.Id);
        var box = await TestData.SeedSnackBoxAsync(_db, oslo.Id);

        var refill = await _refillService.ReportEmptyAsync(box.Id, reporter.Id, 400, null);
        await _refillService.AcceptOfferAsync(refill.Id, volunteer.Id);
        await _refillService.SubmitPurchaseAsync(refill.Id, volunteer.Id, 400, "RCPT-001", "Test");
        await _refillService.ConfirmRefilledAsync(refill.Id, volunteer.Id);

        var claim = await _reimbursementService.CreateClaimAsync(refill.Id);
        var submitted = await _reimbursementService.SubmitClaimAsync(claim.Id);

        // Status should be Submitted, not Paid
        Assert.That(submitted.Status, Is.EqualTo(ClaimStatus.Submitted));

        var reloadedRefill = await _db.RefillRequests.FindAsync(refill.Id);
        Assert.That(reloadedRefill!.Status, Is.Not.EqualTo(RefillStatus.Reimbursed));
    }

    // Test 19: Duplicate refill completion does not double-count spending
    [Test]
    public async Task ConfirmRefill_Twice_DoesNotDoubleCount()
    {
        var (oslo, _) = await TestData.SeedOfficesAsync(_db);
        var reporter = await TestData.SeedEmployeeAsync(_db, oslo.Id);
        var volunteer = await TestData.SeedVolunteerAsync(_db, oslo.Id);
        var box = await TestData.SeedSnackBoxAsync(_db, oslo.Id);

        var refill = await _refillService.ReportEmptyAsync(box.Id, reporter.Id, 400, null);
        await _refillService.AcceptOfferAsync(refill.Id, volunteer.Id);
        await _refillService.SubmitPurchaseAsync(refill.Id, volunteer.Id, 400, "RCPT-001", "Test");
        await _refillService.ConfirmRefilledAsync(refill.Id, volunteer.Id);

        // Second confirm should throw
        Assert.ThrowsAsync<InvalidStateTransitionException>(() =>
            _refillService.ConfirmRefilledAsync(refill.Id, volunteer.Id));

        var purchaseCount = await _db.Purchases.CountAsync(p => p.RefillRequestId == refill.Id);
        Assert.That(purchaseCount, Is.EqualTo(1));
    }

    // Test 20: Cancelled unpurchased refill releases its budget reservation
    [Test]
    public async Task Cancel_UnpurchasedRefill_ReleasesReservation()
    {
        var (oslo, _) = await TestData.SeedOfficesAsync(_db);
        var reporter = await TestData.SeedEmployeeAsync(_db, oslo.Id);
        var volunteer = await TestData.SeedVolunteerAsync(_db, oslo.Id);
        var box = await TestData.SeedSnackBoxAsync(_db, oslo.Id);

        var refill = await _refillService.ReportEmptyAsync(box.Id, reporter.Id, 400, null);
        await _refillService.AcceptOfferAsync(refill.Id, volunteer.Id);

        var budgetBefore = await _budgetService.GetBudgetStatusAsync(oslo.Id, _clock.UtcNow.Year, _clock.UtcNow.Month);
        Assert.That(budgetBefore.ActiveReservations, Is.EqualTo(400));

        await _refillService.CancelAsync(refill.Id, "Test cancellation");

        var budgetAfter = await _budgetService.GetBudgetStatusAsync(oslo.Id, _clock.UtcNow.Year, _clock.UtcNow.Month);
        Assert.That(budgetAfter.ActiveReservations, Is.EqualTo(0));
    }
}

[TestFixture]
public class BudgetTests
{
    private SnackCounterDbContext _db = null!;
    private TestClock _clock = null!;
    private BudgetService _budgetService = null!;
    private Mock<IChatNotificationService> _chat = null!;
    private RefillService _refillService = null!;
    private VolunteerService _volunteerService = null!;

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
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    // Test 21: Oslo monthly limit is 6000 NOK
    [Test]
    public async Task Oslo_MonthlyLimit_Is6000Nok()
    {
        var (oslo, _) = await TestData.SeedOfficesAsync(_db);
        var budget = await _budgetService.GetBudgetStatusAsync(oslo.Id, _clock.UtcNow.Year, _clock.UtcNow.Month);
        Assert.That(budget.MonthlyBudget, Is.EqualTo(6000));
        Assert.That(budget.Currency, Is.EqualTo("NOK"));
    }

    // Test 22: Bengaluru monthly limit is 15000 INR
    [Test]
    public async Task Bengaluru_MonthlyLimit_Is15000Inr()
    {
        var (_, bengaluru) = await TestData.SeedOfficesAsync(_db);
        var budget = await _budgetService.GetBudgetStatusAsync(bengaluru.Id, _clock.UtcNow.Year, _clock.UtcNow.Month);
        Assert.That(budget.MonthlyBudget, Is.EqualTo(15000));
        Assert.That(budget.Currency, Is.EqualTo("INR"));
    }

    // Test 23: Budget is shared across volunteers within the same office
    [Test]
    public async Task Budget_SharedAcrossVolunteers()
    {
        var (oslo, _) = await TestData.SeedOfficesAsync(_db);
        var reporter = await TestData.SeedEmployeeAsync(_db, oslo.Id);
        var vol1 = await TestData.SeedVolunteerAsync(_db, oslo.Id, "Vol1");
        var vol2 = await TestData.SeedVolunteerAsync(_db, oslo.Id, "Vol2");
        var box1 = await TestData.SeedSnackBoxAsync(_db, oslo.Id, "Box1");
        var box2 = await TestData.SeedSnackBoxAsync(_db, oslo.Id, "Box2");

        var refill1 = await _refillService.ReportEmptyAsync(box1.Id, reporter.Id, 3000, null);
        await _refillService.AcceptOfferAsync(refill1.Id, vol1.Id);

        var refill2 = await _refillService.ReportEmptyAsync(box2.Id, reporter.Id, 3000, null);
        await _refillService.AcceptOfferAsync(refill2.Id, vol2.Id);

        var budget = await _budgetService.GetBudgetStatusAsync(oslo.Id, _clock.UtcNow.Year, _clock.UtcNow.Month);
        Assert.That(budget.ActiveReservations, Is.EqualTo(6000));
        Assert.That(budget.AvailableBudget, Is.EqualTo(0));
    }

    // Test 24: Spending in Oslo does not reduce Bengaluru budget
    [Test]
    public async Task OsloSpending_DoesNotAffectBengaluruBudget()
    {
        var (oslo, bengaluru) = await TestData.SeedOfficesAsync(_db);
        var reporter = await TestData.SeedEmployeeAsync(_db, oslo.Id);
        var vol = await TestData.SeedVolunteerAsync(_db, oslo.Id);
        var box = await TestData.SeedSnackBoxAsync(_db, oslo.Id);

        var refill = await _refillService.ReportEmptyAsync(box.Id, reporter.Id, 2000, null);
        await _refillService.AcceptOfferAsync(refill.Id, vol.Id);

        var bengaluruBudget = await _budgetService.GetBudgetStatusAsync(bengaluru.Id, _clock.UtcNow.Year, _clock.UtcNow.Month);
        Assert.That(bengaluruBudget.ActiveReservations, Is.EqualTo(0));
        Assert.That(bengaluruBudget.AvailableBudget, Is.EqualTo(15000));
    }

    // Test 25: Active reservations reduce available budget
    [Test]
    public async Task ActiveReservations_ReduceAvailableBudget()
    {
        var (oslo, _) = await TestData.SeedOfficesAsync(_db);
        var reporter = await TestData.SeedEmployeeAsync(_db, oslo.Id);
        var vol = await TestData.SeedVolunteerAsync(_db, oslo.Id);
        var box = await TestData.SeedSnackBoxAsync(_db, oslo.Id);

        var refill = await _refillService.ReportEmptyAsync(box.Id, reporter.Id, 1500, null);
        await _refillService.AcceptOfferAsync(refill.Id, vol.Id);

        var budget = await _budgetService.GetBudgetStatusAsync(oslo.Id, _clock.UtcNow.Year, _clock.UtcNow.Month);
        Assert.That(budget.AvailableBudget, Is.EqualTo(4500));
    }

    // Test 26: Actual purchase amount replaces estimated reservation
    [Test]
    public async Task ActualPurchase_ReplacesEstimatedReservation()
    {
        var (oslo, _) = await TestData.SeedOfficesAsync(_db);
        var reporter = await TestData.SeedEmployeeAsync(_db, oslo.Id);
        var vol = await TestData.SeedVolunteerAsync(_db, oslo.Id);
        var box = await TestData.SeedSnackBoxAsync(_db, oslo.Id);

        var refill = await _refillService.ReportEmptyAsync(box.Id, reporter.Id, 500, null);
        await _refillService.AcceptOfferAsync(refill.Id, vol.Id);

        var budgetAfterReserve = await _budgetService.GetBudgetStatusAsync(oslo.Id, _clock.UtcNow.Year, _clock.UtcNow.Month);
        Assert.That(budgetAfterReserve.ActiveReservations, Is.EqualTo(500));

        await _refillService.SubmitPurchaseAsync(refill.Id, vol.Id, 450, "RCPT-001", "Test");

        var budgetAfterPurchase = await _budgetService.GetBudgetStatusAsync(oslo.Id, _clock.UtcNow.Year, _clock.UtcNow.Month);
        Assert.That(budgetAfterPurchase.ActiveReservations, Is.EqualTo(0));
        Assert.That(budgetAfterPurchase.ActualSpending, Is.EqualTo(450));
    }

    // Test 27: Negative and zero amounts are rejected
    [Test]
    public async Task NegativeAmount_IsRejected()
    {
        var (oslo, _) = await TestData.SeedOfficesAsync(_db);
        var reporter = await TestData.SeedEmployeeAsync(_db, oslo.Id);
        var vol = await TestData.SeedVolunteerAsync(_db, oslo.Id);
        var box = await TestData.SeedSnackBoxAsync(_db, oslo.Id);

        var refill = await _refillService.ReportEmptyAsync(box.Id, reporter.Id, 400, null);
        await _refillService.AcceptOfferAsync(refill.Id, vol.Id);

        Assert.ThrowsAsync<ValidationException>(() =>
            _refillService.SubmitPurchaseAsync(refill.Id, vol.Id, -100, "RCPT", "Test"));
        Assert.ThrowsAsync<ValidationException>(() =>
            _refillService.SubmitPurchaseAsync(refill.Id, vol.Id, 0, "RCPT", "Test"));
    }

    // Test 28: Different months maintain independent budgets
    [Test]
    public async Task DifferentMonths_IndependentBudgets()
    {
        var (oslo, _) = await TestData.SeedOfficesAsync(_db);

        // Seed a purchase in a previous month
        var reporter = await TestData.SeedEmployeeAsync(_db, oslo.Id);
        var vol = await TestData.SeedVolunteerAsync(_db, oslo.Id);
        var box = await TestData.SeedSnackBoxAsync(_db, oslo.Id);

        var lastMonth = _clock.UtcNow.AddMonths(-1);
        var purchase = new Purchase
        {
            RefillRequestId = 0,
            EmployeeId = vol.Id,
            Amount = 5000,
            Currency = "NOK",
            PurchasedAt = lastMonth,
            Description = "Last month purchase"
        };

        // Add a refill for the purchase FK
        var refill = new RefillRequest
        {
            SnackBoxId = box.Id, OfficeId = oslo.Id, Status = RefillStatus.Reimbursed,
            EstimatedCost = 5000, ReportedByEmployeeId = reporter.Id, CreatedAt = lastMonth
        };
        _db.RefillRequests.Add(refill);
        await _db.SaveChangesAsync();
        purchase.RefillRequestId = refill.Id;
        _db.Purchases.Add(purchase);
        await _db.SaveChangesAsync();

        // Current month budget should be unaffected
        var budget = await _budgetService.GetBudgetStatusAsync(oslo.Id, _clock.UtcNow.Year, _clock.UtcNow.Month);
        Assert.That(budget.ActualSpending, Is.EqualTo(0));
        Assert.That(budget.AvailableBudget, Is.EqualTo(6000));
    }

    // Test 30: Concurrent purchases cannot overspend the same budget
    [Test]
    public async Task ConcurrentReservations_CannotOverspend()
    {
        var (oslo, _) = await TestData.SeedOfficesAsync(_db);

        // Try to reserve 4000 + 4000 = 8000 against a 6000 budget
        var task1 = _budgetService.ReserveBudgetAsync(1001, oslo.Id, 4000);
        var task2 = _budgetService.ReserveBudgetAsync(1002, oslo.Id, 4000);

        Exception? ex = null;
        try { await Task.WhenAll(task1, task2); }
        catch (Exception e) { ex = e; }

        // At least one should fail
        var totalReserved = await _db.BudgetReservations
            .Where(r => r.OfficeId == oslo.Id && r.Status == ReservationStatus.Active)
            .SumAsync(r => r.Amount);

        Assert.That(totalReserved, Is.LessThanOrEqualTo(6000));
    }
}
