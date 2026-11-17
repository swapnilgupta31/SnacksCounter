using Microsoft.EntityFrameworkCore;
using SnackCounter.Core.Entities;

namespace SnackCounter.Infrastructure.Data;

public class SnackCounterDbContext(DbContextOptions<SnackCounterDbContext> options) : DbContext(options)
{
    public DbSet<Office> Offices => Set<Office>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<SnackBox> SnackBoxes => Set<SnackBox>();
    public DbSet<RefillRequest> RefillRequests => Set<RefillRequest>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<ReimbursementClaim> ReimbursementClaims => Set<ReimbursementClaim>();
    public DbSet<BudgetReservation> BudgetReservations => Set<BudgetReservation>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ExchangeRateSnapshot> ExchangeRateSnapshots => Set<ExchangeRateSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SnackCounterDbContext).Assembly);
    }
}
