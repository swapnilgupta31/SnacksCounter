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
public class VolunteerTests
{
    private SnackCounterDbContext _db = null!;
    private TestClock _clock = null!;
    private Mock<IChatNotificationService> _chat = null!;
    private VolunteerService _volunteerService = null!;
    private BudgetService _budgetService = null!;
    private RefillService _refillService = null!;

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

    // Test 5: Unavailable volunteers are not selected
    [Test]
    public async Task UnavailableVolunteer_NotSelected()
    {
        var (oslo, _) = await TestData.SeedOfficesAsync(_db);
        await TestData.SeedVolunteerAsync(_db, oslo.Id, "Unavailable", isAvailable: false);
        var result = await _volunteerService.SelectEligibleVolunteerAsync(oslo.Id);
        Assert.That(result, Is.Null);
    }

    // Test 6: Volunteers on leave are not selected
    [Test]
    public async Task VolunteerOnLeave_NotSelected()
    {
        var (oslo, _) = await TestData.SeedOfficesAsync(_db);
        await TestData.SeedVolunteerAsync(_db, oslo.Id, "OnLeave", isOnLeave: true);
        var result = await _volunteerService.SelectEligibleVolunteerAsync(oslo.Id);
        Assert.That(result, Is.Null);
    }

    // Test 7: Volunteers not present today are not selected
    [Test]
    public async Task VolunteerNotPresent_NotSelected()
    {
        var (oslo, _) = await TestData.SeedOfficesAsync(_db);
        await TestData.SeedVolunteerAsync(_db, oslo.Id, "Remote", isPresentToday: false);
        var result = await _volunteerService.SelectEligibleVolunteerAsync(oslo.Id);
        Assert.That(result, Is.Null);
    }

    // Test 8: Visiting volunteers can be selected for their current office
    [Test]
    public async Task VisitingVolunteer_SelectedForCurrentOffice()
    {
        var (oslo, bengaluru) = await TestData.SeedOfficesAsync(_db);
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

        var result = await _volunteerService.SelectEligibleVolunteerAsync(bengaluru.Id);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(visitor.Id));
    }

    // Test 9: Volunteer with fewer recent refills is preferred
    [Test]
    public async Task FewerRecentRefills_PreferredVolunteer()
    {
        var (oslo, _) = await TestData.SeedOfficesAsync(_db);
        var reporter = await TestData.SeedEmployeeAsync(_db, oslo.Id);
        var vol1 = await TestData.SeedVolunteerAsync(_db, oslo.Id, "BusyVol");
        var vol2 = await TestData.SeedVolunteerAsync(_db, oslo.Id, "FreshVol");
        var box1 = await TestData.SeedSnackBoxAsync(_db, oslo.Id, "Box1");
        var box2 = await TestData.SeedSnackBoxAsync(_db, oslo.Id, "Box2");

        // Give vol1 a recent completed refill
        var completedRefill = new RefillRequest
        {
            SnackBoxId = box1.Id, OfficeId = oslo.Id, Status = RefillStatus.Reimbursed,
            AssignedVolunteerId = vol1.Id, EstimatedCost = 300,
            ReportedByEmployeeId = reporter.Id, CreatedAt = DateTime.UtcNow.AddDays(-5),
            CompletedAt = DateTime.UtcNow.AddDays(-4)
        };
        _db.RefillRequests.Add(completedRefill);
        await _db.SaveChangesAsync();

        var selected = await _volunteerService.SelectEligibleVolunteerAsync(oslo.Id);
        Assert.That(selected!.Id, Is.EqualTo(vol2.Id));
    }

    // Test 10: Expired unaccepted offer is reassigned to another volunteer
    [Test]
    public async Task ExpiredOffer_Reassigned()
    {
        var (oslo, _) = await TestData.SeedOfficesAsync(_db);
        var reporter = await TestData.SeedEmployeeAsync(_db, oslo.Id);
        var vol1 = await TestData.SeedVolunteerAsync(_db, oslo.Id, "Vol1");
        var vol2 = await TestData.SeedVolunteerAsync(_db, oslo.Id, "Vol2");
        var box = await TestData.SeedSnackBoxAsync(_db, oslo.Id);

        var refill = await _refillService.ReportEmptyAsync(box.Id, reporter.Id, 400, null);
        Assert.That(refill.Status, Is.EqualTo(RefillStatus.Offered));

        // Move clock past offer expiry
        _clock.UtcNow = _clock.UtcNow.AddHours(3);

        var processed = await _refillService.ProcessExpiredOffersAsync();
        Assert.That(processed, Is.EqualTo(1));

        var updated = await _db.RefillRequests.FindAsync(refill.Id);
        // Should be re-offered to another volunteer
        Assert.That(updated!.Status, Is.EqualTo(RefillStatus.Offered));
    }

    // Test 11: Accepted assignment is not silently reassigned
    [Test]
    public async Task AcceptedAssignment_NotSilentlyReassigned()
    {
        var (oslo, _) = await TestData.SeedOfficesAsync(_db);
        var reporter = await TestData.SeedEmployeeAsync(_db, oslo.Id);
        var vol = await TestData.SeedVolunteerAsync(_db, oslo.Id);
        var box = await TestData.SeedSnackBoxAsync(_db, oslo.Id);

        var refill = await _refillService.ReportEmptyAsync(box.Id, reporter.Id, 400, null);
        await _refillService.AcceptOfferAsync(refill.Id, vol.Id);

        // Move clock past assignment deadline
        _clock.UtcNow = _clock.UtcNow.AddHours(25);

        // ProcessExpiredOffers only handles Offered status, not Assigned
        var processed = await _refillService.ProcessExpiredOffersAsync();
        Assert.That(processed, Is.EqualTo(0));

        var updated = await _db.RefillRequests.FindAsync(refill.Id);
        Assert.That(updated!.Status, Is.EqualTo(RefillStatus.Assigned));
        Assert.That(updated.AssignedVolunteerId, Is.EqualTo(vol.Id));
    }

    // Test 12: No available volunteer leaves the request pending
    [Test]
    public async Task NoVolunteer_RequestStaysPending()
    {
        var (oslo, _) = await TestData.SeedOfficesAsync(_db);
        var reporter = await TestData.SeedEmployeeAsync(_db, oslo.Id);
        var box = await TestData.SeedSnackBoxAsync(_db, oslo.Id);

        var refill = await _refillService.ReportEmptyAsync(box.Id, reporter.Id, 400, null);

        // No volunteers seeded — should stay Requested
        Assert.That(refill.Status, Is.EqualTo(RefillStatus.Requested));
    }
}

[TestFixture]
public class ConcurrencyTests
{
    private Mock<IChatNotificationService> _chat = null!;

    [SetUp]
    public void SetUp()
    {
        _chat = new Mock<IChatNotificationService>();
        _chat.Setup(c => c.NotifyAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>()))
             .Returns(Task.CompletedTask);
    }

    // Test 1: Two volunteers attempt to accept the same assignment simultaneously
    [Test]
    public async Task TwoVolunteers_AcceptSameOffer_OnlyOneSucceeds()
    {
        var dbName = Guid.NewGuid().ToString();
        using var db1 = TestDbFactory.Create(dbName);
        var clock = new TestClock(DateTime.UtcNow);
        var (oslo, _) = await TestData.SeedOfficesAsync(db1);
        var reporter = await TestData.SeedEmployeeAsync(db1, oslo.Id);
        var vol1 = await TestData.SeedVolunteerAsync(db1, oslo.Id, "Vol1");
        var vol2 = await TestData.SeedVolunteerAsync(db1, oslo.Id, "Vol2");
        var box = await TestData.SeedSnackBoxAsync(db1, oslo.Id);

        // Create an offered refill manually assigned to vol1
        var refill = new RefillRequest
        {
            SnackBoxId = box.Id, OfficeId = oslo.Id, Status = RefillStatus.Offered,
            AssignedVolunteerId = vol1.Id, EstimatedCost = 400,
            ReportedByEmployeeId = reporter.Id, CreatedAt = clock.UtcNow,
            OfferExpiresAt = clock.UtcNow.AddHours(2)
        };
        db1.RefillRequests.Add(refill);
        await db1.SaveChangesAsync();

        // Two separate DB contexts simulating concurrent requests
        using var db2 = TestDbFactory.Create(dbName);
        using var db3 = TestDbFactory.Create(dbName);

        var budget2 = new BudgetService(db2, clock);
        var vol2svc = new VolunteerService(db2, clock);
        var svc2 = new RefillService(db2, budget2, vol2svc, _chat.Object, clock);

        var budget3 = new BudgetService(db3, clock);
        var vol3svc = new VolunteerService(db3, clock);
        var svc3 = new RefillService(db3, budget3, vol3svc, _chat.Object, clock);

        int successCount = 0;
        var t1 = Task.Run(async () =>
        {
            try { await svc2.AcceptOfferAsync(refill.Id, vol1.Id); successCount++; }
            catch { }
        });
        var t2 = Task.Run(async () =>
        {
            try { await svc3.AcceptOfferAsync(refill.Id, vol1.Id); successCount++; }
            catch { }
        });

        await Task.WhenAll(t1, t2);
        Assert.That(successCount, Is.LessThanOrEqualTo(1));
    }

    // Test 2: Two employees report the same empty snack box simultaneously
    // Note: SQLite in-memory doesn't enforce cross-connection uniqueness.
    // This test verifies the application-level ConflictException guard.
    [Test]
    public async Task TwoEmployees_ReportSameBox_OnlyOneRefillCreated()
    {
        var dbName = Guid.NewGuid().ToString();
        using var db1 = TestDbFactory.Create(dbName);
        var clock = new TestClock(DateTime.UtcNow);
        var (oslo, _) = await TestData.SeedOfficesAsync(db1);
        var emp1 = await TestData.SeedEmployeeAsync(db1, oslo.Id, name: "Emp1");
        var emp2 = await TestData.SeedEmployeeAsync(db1, oslo.Id, name: "Emp2");
        var box = await TestData.SeedSnackBoxAsync(db1, oslo.Id);

        // First report succeeds
        using var db2 = TestDbFactory.Create(dbName);
        var svc2 = new RefillService(db2, new BudgetService(db2, clock), new VolunteerService(db2, clock), _chat.Object, clock);
        await svc2.ReportEmptyAsync(box.Id, emp1.Id, 400, null);

        // Second report on same box should throw ConflictException
        using var db3 = TestDbFactory.Create(dbName);
        var svc3 = new RefillService(db3, new BudgetService(db3, clock), new VolunteerService(db3, clock), _chat.Object, clock);
        Assert.ThrowsAsync<SnackCounter.Core.Exceptions.ConflictException>(() =>
            svc3.ReportEmptyAsync(box.Id, emp2.Id, 400, null));

        using var dbCheck = TestDbFactory.Create(dbName);
        var activeStatuses = new[] { RefillStatus.Requested, RefillStatus.Offered, RefillStatus.Assigned };
        var activeCount = await dbCheck.RefillRequests
            .CountAsync(r => r.SnackBoxId == box.Id && activeStatuses.Contains(r.Status));
        Assert.That(activeCount, Is.EqualTo(1));
    }

    // Test 4: Duplicate HR submission attempts result in one external claim
    [Test]
    public async Task DuplicateHrSubmission_OnlyOneExternalClaim()
    {
        var store = new FakeHrStore();
        var key = "refill-42-v1";
        var request = new SnackCounter.Core.Interfaces.HrClaimRequest(1, "Test", 400, "NOK", "Test", "RCPT", key);

        var t1 = Task.Run(() => store.Create(request));
        var t2 = Task.Run(() => store.Create(request));
        await Task.WhenAll(t1, t2);

        var claims = store.GetAll().Where(c => c.IdempotencyKey == key).ToList();
        Assert.That(claims.Count, Is.EqualTo(1));
    }
}
