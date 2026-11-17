using SnackCounter.Core.Entities;
using SnackCounter.Core.Enums;
using SnackCounter.Infrastructure.Data;

namespace SnackCounter.Tests.TestHelpers;

public static class TestData
{
    public static async Task<(Office oslo, Office bengaluru)> SeedOfficesAsync(SnackCounterDbContext db)
    {
        var oslo = new Office { Name = "Oslo", Currency = "NOK", TimeZoneId = "Europe/Oslo", MonthlyBudget = 6000 };
        var bengaluru = new Office { Name = "Bengaluru", Currency = "INR", TimeZoneId = "Asia/Kolkata", MonthlyBudget = 15000 };
        db.Offices.AddRange(oslo, bengaluru);
        await db.SaveChangesAsync();
        return (oslo, bengaluru);
    }

    public static async Task<Employee> SeedVolunteerAsync(SnackCounterDbContext db, int officeId, string name = "Test Volunteer", bool isAvailable = true, bool isOnLeave = false, bool isPresentToday = true)
    {
        var emp = new Employee
        {
            Name = name,
            Email = $"{name.Replace(" ", "").ToLower()}@test.com",
            HomeOfficeId = officeId,
            CurrentOfficeId = officeId,
            PayrollCurrency = "NOK",
            IsVolunteer = true,
            IsAvailable = isAvailable,
            IsOnLeave = isOnLeave,
            IsPresentToday = isPresentToday,
            Role = "Volunteer"
        };
        db.Employees.Add(emp);
        await db.SaveChangesAsync();
        return emp;
    }

    public static async Task<Employee> SeedEmployeeAsync(SnackCounterDbContext db, int homeOfficeId, int? currentOfficeId = null, string name = "Test Employee")
    {
        var emp = new Employee
        {
            Name = name,
            Email = $"{name.Replace(" ", "").ToLower()}{Guid.NewGuid():N}@test.com",
            HomeOfficeId = homeOfficeId,
            CurrentOfficeId = currentOfficeId ?? homeOfficeId,
            PayrollCurrency = "NOK",
            IsVolunteer = false,
            IsAvailable = true,
            IsOnLeave = false,
            IsPresentToday = true,
            Role = "Employee"
        };
        db.Employees.Add(emp);
        await db.SaveChangesAsync();
        return emp;
    }

    public static async Task<SnackBox> SeedSnackBoxAsync(SnackCounterDbContext db, int officeId, string name = "Test Snack Box")
    {
        var box = new SnackBox
        {
            OfficeId = officeId,
            Name = name,
            Category = "Biscuits",
            AvailabilityStatus = SnackAvailability.Available
        };
        db.SnackBoxes.Add(box);
        await db.SaveChangesAsync();
        return box;
    }

    public static async Task<RefillRequest> SeedAssignedRefillAsync(SnackCounterDbContext db, int snackBoxId, int officeId, int volunteerId, int reportedById, DateTime? deadline = null, decimal estimatedCost = 500)
    {
        var refill = new RefillRequest
        {
            SnackBoxId = snackBoxId,
            OfficeId = officeId,
            Status = RefillStatus.Assigned,
            AssignedVolunteerId = volunteerId,
            EstimatedCost = estimatedCost,
            ReportedByEmployeeId = reportedById,
            CreatedAt = DateTime.UtcNow,
            AssignedAt = DateTime.UtcNow,
            AssignmentDeadline = deadline ?? DateTime.UtcNow.AddHours(24)
        };
        db.RefillRequests.Add(refill);

        var reservation = new BudgetReservation
        {
            RefillRequestId = refill.Id,
            OfficeId = officeId,
            Amount = estimatedCost,
            Currency = "NOK",
            BudgetMonth = DateTime.UtcNow.ToString("yyyy-MM"),
            Status = ReservationStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        db.BudgetReservations.Add(reservation);
        await db.SaveChangesAsync();

        // Fix the FK after save
        reservation.RefillRequestId = refill.Id;
        await db.SaveChangesAsync();

        return refill;
    }
}
