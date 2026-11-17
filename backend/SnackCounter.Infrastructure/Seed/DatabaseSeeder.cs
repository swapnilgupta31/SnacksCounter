using Microsoft.EntityFrameworkCore;
using SnackCounter.Core.Entities;
using SnackCounter.Core.Enums;
using SnackCounter.Infrastructure.Data;

namespace SnackCounter.Infrastructure.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(SnackCounterDbContext db)
    {
        if (await db.Offices.AnyAsync()) return;

        var oslo = new Office { Name = "Oslo", Currency = "NOK", TimeZoneId = "Europe/Oslo", MonthlyBudget = 6000 };
        var bengaluru = new Office { Name = "Bengaluru", Currency = "INR", TimeZoneId = "Asia/Kolkata", MonthlyBudget = 15000 };
        db.Offices.AddRange(oslo, bengaluru);
        await db.SaveChangesAsync();

        var employees = new List<Employee>
        {
            // Oslo employees
            new() { Name = "Erik Andersen", Email = "erik@framsikt.no", HomeOfficeId = oslo.Id, CurrentOfficeId = oslo.Id, PayrollCurrency = "NOK", IsVolunteer = true, IsAvailable = true, IsOnLeave = false, IsPresentToday = true, Role = "Volunteer" },
            new() { Name = "Ingrid Berg", Email = "ingrid@framsikt.no", HomeOfficeId = oslo.Id, CurrentOfficeId = oslo.Id, PayrollCurrency = "NOK", IsVolunteer = true, IsAvailable = true, IsOnLeave = false, IsPresentToday = true, Role = "Volunteer" },
            new() { Name = "Lars Christensen", Email = "lars@framsikt.no", HomeOfficeId = oslo.Id, CurrentOfficeId = oslo.Id, PayrollCurrency = "NOK", IsVolunteer = true, IsAvailable = false, IsOnLeave = true, IsPresentToday = false, Role = "Volunteer" },
            new() { Name = "Maja Dahl", Email = "maja@framsikt.no", HomeOfficeId = oslo.Id, CurrentOfficeId = oslo.Id, PayrollCurrency = "NOK", IsVolunteer = true, IsAvailable = true, IsOnLeave = false, IsPresentToday = false, Role = "Volunteer" },
            new() { Name = "Nils Eriksen", Email = "nils@framsikt.no", HomeOfficeId = oslo.Id, CurrentOfficeId = oslo.Id, PayrollCurrency = "NOK", IsVolunteer = true, IsAvailable = true, IsOnLeave = false, IsPresentToday = true, Role = "Volunteer" },
            new() { Name = "Sofie Fjord", Email = "sofie@framsikt.no", HomeOfficeId = oslo.Id, CurrentOfficeId = oslo.Id, PayrollCurrency = "NOK", IsVolunteer = false, IsAvailable = true, IsOnLeave = false, IsPresentToday = true, Role = "Employee" },
            new() { Name = "Tor Grieg", Email = "tor@framsikt.no", HomeOfficeId = oslo.Id, CurrentOfficeId = oslo.Id, PayrollCurrency = "NOK", IsVolunteer = true, IsAvailable = true, IsOnLeave = false, IsPresentToday = true, Role = "Volunteer" },
            new() { Name = "Astrid Hansen", Email = "astrid@framsikt.no", HomeOfficeId = oslo.Id, CurrentOfficeId = oslo.Id, PayrollCurrency = "NOK", IsVolunteer = true, IsAvailable = true, IsOnLeave = false, IsPresentToday = true, Role = "Volunteer" },
            new() { Name = "Bjorn Iversen", Email = "bjorn@framsikt.no", HomeOfficeId = oslo.Id, CurrentOfficeId = oslo.Id, PayrollCurrency = "NOK", IsVolunteer = true, IsAvailable = true, IsOnLeave = false, IsPresentToday = true, Role = "Volunteer" },
            new() { Name = "Camilla Jensen", Email = "camilla@framsikt.no", HomeOfficeId = oslo.Id, CurrentOfficeId = oslo.Id, PayrollCurrency = "NOK", IsVolunteer = false, IsAvailable = true, IsOnLeave = false, IsPresentToday = true, Role = "Employee" },
            new() { Name = "Dag Karlsen", Email = "dag@framsikt.no", HomeOfficeId = oslo.Id, CurrentOfficeId = oslo.Id, PayrollCurrency = "NOK", IsVolunteer = true, IsAvailable = true, IsOnLeave = false, IsPresentToday = true, Role = "Volunteer" },
            new() { Name = "Eva Larsen", Email = "eva@framsikt.no", HomeOfficeId = oslo.Id, CurrentOfficeId = oslo.Id, PayrollCurrency = "NOK", IsVolunteer = true, IsAvailable = true, IsOnLeave = false, IsPresentToday = true, Role = "Volunteer" },
            new() { Name = "Finn Moe", Email = "finn@framsikt.no", HomeOfficeId = oslo.Id, CurrentOfficeId = oslo.Id, PayrollCurrency = "NOK", IsVolunteer = false, IsAvailable = true, IsOnLeave = false, IsPresentToday = true, Role = "Employee" },
            new() { Name = "Grete Nilsen", Email = "grete@framsikt.no", HomeOfficeId = oslo.Id, CurrentOfficeId = oslo.Id, PayrollCurrency = "NOK", IsVolunteer = true, IsAvailable = true, IsOnLeave = false, IsPresentToday = true, Role = "Volunteer" },
            new() { Name = "Hans Olsen", Email = "hans@framsikt.no", HomeOfficeId = oslo.Id, CurrentOfficeId = oslo.Id, PayrollCurrency = "NOK", IsVolunteer = false, IsAvailable = true, IsOnLeave = false, IsPresentToday = true, Role = "Admin" },
            // Bengaluru employees
            new() { Name = "Aarav Sharma", Email = "aarav@framsikt.in", HomeOfficeId = bengaluru.Id, CurrentOfficeId = bengaluru.Id, PayrollCurrency = "INR", IsVolunteer = true, IsAvailable = true, IsOnLeave = false, IsPresentToday = true, Role = "Volunteer" },
            new() { Name = "Priya Patel", Email = "priya@framsikt.in", HomeOfficeId = bengaluru.Id, CurrentOfficeId = bengaluru.Id, PayrollCurrency = "INR", IsVolunteer = true, IsAvailable = true, IsOnLeave = false, IsPresentToday = true, Role = "Volunteer" },
            new() { Name = "Rohan Kumar", Email = "rohan@framsikt.in", HomeOfficeId = bengaluru.Id, CurrentOfficeId = bengaluru.Id, PayrollCurrency = "INR", IsVolunteer = true, IsAvailable = false, IsOnLeave = true, IsPresentToday = false, Role = "Volunteer" },
            new() { Name = "Sneha Reddy", Email = "sneha@framsikt.in", HomeOfficeId = bengaluru.Id, CurrentOfficeId = bengaluru.Id, PayrollCurrency = "INR", IsVolunteer = true, IsAvailable = true, IsOnLeave = false, IsPresentToday = true, Role = "Volunteer" },
            new() { Name = "Vikram Singh", Email = "vikram@framsikt.in", HomeOfficeId = bengaluru.Id, CurrentOfficeId = bengaluru.Id, PayrollCurrency = "INR", IsVolunteer = true, IsAvailable = true, IsOnLeave = false, IsPresentToday = false, Role = "Volunteer" },
            new() { Name = "Ananya Iyer", Email = "ananya@framsikt.in", HomeOfficeId = bengaluru.Id, CurrentOfficeId = bengaluru.Id, PayrollCurrency = "INR", IsVolunteer = false, IsAvailable = true, IsOnLeave = false, IsPresentToday = true, Role = "Employee" },
            new() { Name = "Kiran Nair", Email = "kiran@framsikt.in", HomeOfficeId = bengaluru.Id, CurrentOfficeId = bengaluru.Id, PayrollCurrency = "INR", IsVolunteer = true, IsAvailable = true, IsOnLeave = false, IsPresentToday = true, Role = "Volunteer" },
            new() { Name = "Deepa Menon", Email = "deepa@framsikt.in", HomeOfficeId = bengaluru.Id, CurrentOfficeId = bengaluru.Id, PayrollCurrency = "INR", IsVolunteer = true, IsAvailable = true, IsOnLeave = false, IsPresentToday = true, Role = "Volunteer" },
            new() { Name = "Arjun Rao", Email = "arjun@framsikt.in", HomeOfficeId = bengaluru.Id, CurrentOfficeId = bengaluru.Id, PayrollCurrency = "INR", IsVolunteer = true, IsAvailable = true, IsOnLeave = false, IsPresentToday = true, Role = "Volunteer" },
            new() { Name = "Meera Krishnan", Email = "meera@framsikt.in", HomeOfficeId = bengaluru.Id, CurrentOfficeId = bengaluru.Id, PayrollCurrency = "INR", IsVolunteer = false, IsAvailable = true, IsOnLeave = false, IsPresentToday = true, Role = "Employee" },
            new() { Name = "Suresh Pillai", Email = "suresh@framsikt.in", HomeOfficeId = bengaluru.Id, CurrentOfficeId = bengaluru.Id, PayrollCurrency = "INR", IsVolunteer = true, IsAvailable = true, IsOnLeave = false, IsPresentToday = true, Role = "Volunteer" },
            new() { Name = "Lakshmi Venkat", Email = "lakshmi@framsikt.in", HomeOfficeId = bengaluru.Id, CurrentOfficeId = bengaluru.Id, PayrollCurrency = "INR", IsVolunteer = true, IsAvailable = true, IsOnLeave = false, IsPresentToday = true, Role = "Volunteer" },
            new() { Name = "Rahul Gupta", Email = "rahul@framsikt.in", HomeOfficeId = bengaluru.Id, CurrentOfficeId = bengaluru.Id, PayrollCurrency = "INR", IsVolunteer = true, IsAvailable = true, IsOnLeave = false, IsPresentToday = true, Role = "Volunteer" },
            new() { Name = "Pooja Desai", Email = "pooja@framsikt.in", HomeOfficeId = bengaluru.Id, CurrentOfficeId = bengaluru.Id, PayrollCurrency = "INR", IsVolunteer = false, IsAvailable = true, IsOnLeave = false, IsPresentToday = true, Role = "Employee" },
            new() { Name = "Amit Joshi", Email = "amit@framsikt.in", HomeOfficeId = bengaluru.Id, CurrentOfficeId = bengaluru.Id, PayrollCurrency = "INR", IsVolunteer = true, IsAvailable = true, IsOnLeave = false, IsPresentToday = true, Role = "Volunteer" },
            new() { Name = "Divya Bhat", Email = "divya@framsikt.in", HomeOfficeId = bengaluru.Id, CurrentOfficeId = bengaluru.Id, PayrollCurrency = "INR", IsVolunteer = false, IsAvailable = true, IsOnLeave = false, IsPresentToday = true, Role = "Admin" },
            // Visiting employee: Oslo employee currently in Bengaluru
            new() { Name = "Olav Strand", Email = "olav@framsikt.no", HomeOfficeId = oslo.Id, CurrentOfficeId = bengaluru.Id, PayrollCurrency = "NOK", IsVolunteer = true, IsAvailable = true, IsOnLeave = false, IsPresentToday = true, Role = "Volunteer" },
        };
        db.Employees.AddRange(employees);
        await db.SaveChangesAsync();

        var osloBoxes = new List<SnackBox>
        {
            new() { OfficeId = oslo.Id, Name = "Digestive Biscuits", Category = "Biscuits", AvailabilityStatus = SnackAvailability.Available, LastRefilledAt = DateTime.UtcNow.AddDays(-5) },
            new() { OfficeId = oslo.Id, Name = "Chocolate Chip Cookies", Category = "Biscuits", AvailabilityStatus = SnackAvailability.Available, LastRefilledAt = DateTime.UtcNow.AddDays(-3) },
            new() { OfficeId = oslo.Id, Name = "Potato Chips", Category = "Chips", AvailabilityStatus = SnackAvailability.Empty },
            new() { OfficeId = oslo.Id, Name = "Corn Chips", Category = "Chips", AvailabilityStatus = SnackAvailability.Available, LastRefilledAt = DateTime.UtcNow.AddDays(-7) },
            new() { OfficeId = oslo.Id, Name = "Dark Chocolate", Category = "Chocolates", AvailabilityStatus = SnackAvailability.Available, LastRefilledAt = DateTime.UtcNow.AddDays(-2) },
            new() { OfficeId = oslo.Id, Name = "Milk Chocolate", Category = "Chocolates", AvailabilityStatus = SnackAvailability.Available, LastRefilledAt = DateTime.UtcNow.AddDays(-4) },
            new() { OfficeId = oslo.Id, Name = "Mixed Nuts", Category = "Nuts", AvailabilityStatus = SnackAvailability.Available, LastRefilledAt = DateTime.UtcNow.AddDays(-6) },
            new() { OfficeId = oslo.Id, Name = "Almonds", Category = "Nuts", AvailabilityStatus = SnackAvailability.Available, LastRefilledAt = DateTime.UtcNow.AddDays(-8) },
            new() { OfficeId = oslo.Id, Name = "Sparkling Water", Category = "Beverages", AvailabilityStatus = SnackAvailability.Available, LastRefilledAt = DateTime.UtcNow.AddDays(-1) },
            new() { OfficeId = oslo.Id, Name = "Fruit Juice", Category = "Beverages", AvailabilityStatus = SnackAvailability.Empty },
        };

        var bengaluruBoxes = new List<SnackBox>
        {
            new() { OfficeId = bengaluru.Id, Name = "Marie Biscuits", Category = "Biscuits", AvailabilityStatus = SnackAvailability.Available, LastRefilledAt = DateTime.UtcNow.AddDays(-4) },
            new() { OfficeId = bengaluru.Id, Name = "Parle-G", Category = "Biscuits", AvailabilityStatus = SnackAvailability.Available, LastRefilledAt = DateTime.UtcNow.AddDays(-2) },
            new() { OfficeId = bengaluru.Id, Name = "Lays Classic", Category = "Chips", AvailabilityStatus = SnackAvailability.Empty },
            new() { OfficeId = bengaluru.Id, Name = "Kurkure", Category = "Chips", AvailabilityStatus = SnackAvailability.Available, LastRefilledAt = DateTime.UtcNow.AddDays(-3) },
            new() { OfficeId = bengaluru.Id, Name = "Dairy Milk", Category = "Chocolates", AvailabilityStatus = SnackAvailability.Available, LastRefilledAt = DateTime.UtcNow.AddDays(-5) },
            new() { OfficeId = bengaluru.Id, Name = "KitKat", Category = "Chocolates", AvailabilityStatus = SnackAvailability.Available, LastRefilledAt = DateTime.UtcNow.AddDays(-6) },
            new() { OfficeId = bengaluru.Id, Name = "Cashews", Category = "Nuts", AvailabilityStatus = SnackAvailability.Available, LastRefilledAt = DateTime.UtcNow.AddDays(-7) },
            new() { OfficeId = bengaluru.Id, Name = "Peanuts", Category = "Nuts", AvailabilityStatus = SnackAvailability.Empty },
            new() { OfficeId = bengaluru.Id, Name = "Nimbooz", Category = "Beverages", AvailabilityStatus = SnackAvailability.Available, LastRefilledAt = DateTime.UtcNow.AddDays(-1) },
            new() { OfficeId = bengaluru.Id, Name = "Mango Frooti", Category = "Beverages", AvailabilityStatus = SnackAvailability.Available, LastRefilledAt = DateTime.UtcNow.AddDays(-3) },
            new() { OfficeId = bengaluru.Id, Name = "Haldiram Mixture", Category = "Chips", AvailabilityStatus = SnackAvailability.Available, LastRefilledAt = DateTime.UtcNow.AddDays(-2) },
            new() { OfficeId = bengaluru.Id, Name = "Oreo Cookies", Category = "Biscuits", AvailabilityStatus = SnackAvailability.Available, LastRefilledAt = DateTime.UtcNow.AddDays(-4) },
        };

        db.SnackBoxes.AddRange(osloBoxes);
        db.SnackBoxes.AddRange(bengaluruBoxes);
        await db.SaveChangesAsync();

        // Historical refills for Oslo
        var erikId = employees.First(e => e.Email == "erik@framsikt.no").Id;
        var ingridId = employees.First(e => e.Email == "ingrid@framsikt.no").Id;
        var aaravId = employees.First(e => e.Email == "aarav@framsikt.in").Id;
        var priyaId = employees.First(e => e.Email == "priya@framsikt.in").Id;
        var hansId = employees.First(e => e.Email == "hans@framsikt.no").Id;

        var historicalOsloRefill = new RefillRequest
        {
            SnackBoxId = osloBoxes[0].Id, OfficeId = oslo.Id, Status = RefillStatus.Reimbursed,
            AssignedVolunteerId = erikId, EstimatedCost = 350, ReportedByEmployeeId = hansId,
            CreatedAt = DateTime.UtcNow.AddDays(-20), AssignedAt = DateTime.UtcNow.AddDays(-20),
            AssignmentDeadline = DateTime.UtcNow.AddDays(-19), CompletedAt = DateTime.UtcNow.AddDays(-19)
        };
        var historicalOsloRefill2 = new RefillRequest
        {
            SnackBoxId = osloBoxes[4].Id, OfficeId = oslo.Id, Status = RefillStatus.Reimbursed,
            AssignedVolunteerId = ingridId, EstimatedCost = 450, ReportedByEmployeeId = hansId,
            CreatedAt = DateTime.UtcNow.AddDays(-15), AssignedAt = DateTime.UtcNow.AddDays(-15),
            AssignmentDeadline = DateTime.UtcNow.AddDays(-14), CompletedAt = DateTime.UtcNow.AddDays(-14)
        };
        var historicalBengaluruRefill = new RefillRequest
        {
            SnackBoxId = bengaluruBoxes[0].Id, OfficeId = bengaluru.Id, Status = RefillStatus.Reimbursed,
            AssignedVolunteerId = aaravId, EstimatedCost = 800, ReportedByEmployeeId = aaravId,
            CreatedAt = DateTime.UtcNow.AddDays(-18), AssignedAt = DateTime.UtcNow.AddDays(-18),
            AssignmentDeadline = DateTime.UtcNow.AddDays(-17), CompletedAt = DateTime.UtcNow.AddDays(-17)
        };

        db.RefillRequests.AddRange(historicalOsloRefill, historicalOsloRefill2, historicalBengaluruRefill);
        await db.SaveChangesAsync();

        var purchases = new List<Purchase>
        {
            new() { RefillRequestId = historicalOsloRefill.Id, EmployeeId = erikId, Amount = 340, Currency = "NOK", PurchasedAt = DateTime.UtcNow.AddDays(-19), ReceiptReference = "RCPT-001", Description = "Digestive Biscuits refill" },
            new() { RefillRequestId = historicalOsloRefill2.Id, EmployeeId = ingridId, Amount = 420, Currency = "NOK", PurchasedAt = DateTime.UtcNow.AddDays(-14), ReceiptReference = "RCPT-002", Description = "Dark Chocolate refill" },
            new() { RefillRequestId = historicalBengaluruRefill.Id, EmployeeId = aaravId, Amount = 750, Currency = "INR", PurchasedAt = DateTime.UtcNow.AddDays(-17), ReceiptReference = "RCPT-003", Description = "Marie Biscuits refill" },
        };
        db.Purchases.AddRange(purchases);
        await db.SaveChangesAsync();

        var claims = new List<ReimbursementClaim>
        {
            new() { RefillRequestId = historicalOsloRefill.Id, EmployeeId = erikId, ExternalClaimId = "HR-2024-001", Amount = 340, Currency = "NOK", Status = ClaimStatus.Paid, IdempotencyKey = $"refill-{historicalOsloRefill.Id}-v1", SubmittedAt = DateTime.UtcNow.AddDays(-19), ApprovedAt = DateTime.UtcNow.AddDays(-17), PaidAt = DateTime.UtcNow.AddDays(-15) },
            new() { RefillRequestId = historicalOsloRefill2.Id, EmployeeId = ingridId, ExternalClaimId = "HR-2024-002", Amount = 420, Currency = "NOK", Status = ClaimStatus.Approved, IdempotencyKey = $"refill-{historicalOsloRefill2.Id}-v1", SubmittedAt = DateTime.UtcNow.AddDays(-14), ApprovedAt = DateTime.UtcNow.AddDays(-12) },
            new() { RefillRequestId = historicalBengaluruRefill.Id, EmployeeId = aaravId, ExternalClaimId = "HR-2024-003", Amount = 750, Currency = "INR", Status = ClaimStatus.Submitted, IdempotencyKey = $"refill-{historicalBengaluruRefill.Id}-v1", SubmittedAt = DateTime.UtcNow.AddDays(-17) },
        };
        db.ReimbursementClaims.AddRange(claims);
        await db.SaveChangesAsync();

        var reservations = new List<BudgetReservation>
        {
            new() { RefillRequestId = historicalOsloRefill.Id, OfficeId = oslo.Id, Amount = 340, Currency = "NOK", BudgetMonth = DateTime.UtcNow.AddDays(-20).ToString("yyyy-MM"), Status = ReservationStatus.Converted },
            new() { RefillRequestId = historicalOsloRefill2.Id, OfficeId = oslo.Id, Amount = 420, Currency = "NOK", BudgetMonth = DateTime.UtcNow.AddDays(-15).ToString("yyyy-MM"), Status = ReservationStatus.Converted },
            new() { RefillRequestId = historicalBengaluruRefill.Id, OfficeId = bengaluru.Id, Amount = 750, Currency = "INR", BudgetMonth = DateTime.UtcNow.AddDays(-18).ToString("yyyy-MM"), Status = ReservationStatus.Converted },
        };
        db.BudgetReservations.AddRange(reservations);
        await db.SaveChangesAsync();

        // Exchange rate snapshot
        var now = DateTime.UtcNow;
        db.ExchangeRateSnapshots.Add(new ExchangeRateSnapshot
        {
            FromCurrency = "NOK", ToCurrency = "INR", Rate = 9m,
            ReportMonth = now.ToString("yyyy-MM"), Label = "DEMO_ILLUSTRATIVE_RATE",
            RecordedAt = now
        });

        // Sample notifications
        db.Notifications.AddRange(
            new Notification { RecipientEmployeeId = erikId, RefillRequestId = historicalOsloRefill.Id, Type = "AssignmentOffer", Message = "You have been offered a snack refill assignment for Digestive Biscuits.", SentAt = DateTime.UtcNow.AddDays(-20) },
            new Notification { RecipientEmployeeId = erikId, RefillRequestId = historicalOsloRefill.Id, Type = "ClaimPaid", Message = "Your reimbursement claim HR-2024-001 has been paid.", SentAt = DateTime.UtcNow.AddDays(-15) },
            new Notification { RecipientEmployeeId = aaravId, RefillRequestId = historicalBengaluruRefill.Id, Type = "ClaimSubmitted", Message = "Your reimbursement claim HR-2024-003 has been submitted to HR.", SentAt = DateTime.UtcNow.AddDays(-17) }
        );

        await db.SaveChangesAsync();
    }
}
