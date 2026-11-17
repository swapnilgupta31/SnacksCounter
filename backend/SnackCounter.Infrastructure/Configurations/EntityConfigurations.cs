using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnackCounter.Core.Entities;
using SnackCounter.Core.Enums;

namespace SnackCounter.Infrastructure.Configurations;

public class OfficeConfiguration : IEntityTypeConfiguration<Office>
{
    public void Configure(EntityTypeBuilder<Office> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Name).IsRequired().HasMaxLength(100);
        builder.Property(o => o.Currency).IsRequired().HasMaxLength(3);
        builder.Property(o => o.TimeZoneId).IsRequired().HasMaxLength(50);
        builder.Property(o => o.MonthlyBudget).HasColumnType("decimal(18,2)");
    }
}

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Email).IsRequired().HasMaxLength(200);
        builder.Property(e => e.PayrollCurrency).IsRequired().HasMaxLength(3);
        builder.Property(e => e.Role).IsRequired().HasMaxLength(20);

        builder.HasOne(e => e.HomeOffice)
            .WithMany()
            .HasForeignKey(e => e.HomeOfficeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CurrentOffice)
            .WithMany(o => o.Employees)
            .HasForeignKey(e => e.CurrentOfficeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.Email).IsUnique();
    }
}

public class SnackBoxConfiguration : IEntityTypeConfiguration<SnackBox>
{
    public void Configure(EntityTypeBuilder<SnackBox> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Category).IsRequired().HasMaxLength(50);

        builder.HasOne(s => s.Office)
            .WithMany(o => o.SnackBoxes)
            .HasForeignKey(s => s.OfficeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RefillRequestConfiguration : IEntityTypeConfiguration<RefillRequest>
{
    public void Configure(EntityTypeBuilder<RefillRequest> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.EstimatedCost).HasColumnType("decimal(18,2)");
        builder.Property(r => r.Notes).HasMaxLength(500);
        builder.Property(r => r.CancellationReason).HasMaxLength(500);

        builder.HasOne(r => r.SnackBox)
            .WithMany(s => s.RefillRequests)
            .HasForeignKey(r => r.SnackBoxId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Office)
            .WithMany()
            .HasForeignKey(r => r.OfficeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.AssignedVolunteer)
            .WithMany(e => e.AssignedRefills)
            .HasForeignKey(r => r.AssignedVolunteerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.ReportedBy)
            .WithMany()
            .HasForeignKey(r => r.ReportedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Application-level duplicate check in RefillService is the primary guard.
        // A non-unique index on SnackBoxId for query performance:
        builder.HasIndex(r => r.SnackBoxId)
            .HasDatabaseName("IX_RefillRequest_SnackBoxId");
    }
}

public class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Amount).HasColumnType("decimal(18,2)");
        builder.Property(p => p.Currency).IsRequired().HasMaxLength(3);
        builder.Property(p => p.Description).IsRequired().HasMaxLength(500);
        builder.Property(p => p.ReceiptReference).HasMaxLength(500);

        builder.HasOne(p => p.RefillRequest)
            .WithOne(r => r.Purchase)
            .HasForeignKey<Purchase>(p => p.RefillRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Employee)
            .WithMany(e => e.Purchases)
            .HasForeignKey(p => p.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.RefillRequestId).IsUnique();
    }
}

public class ReimbursementClaimConfiguration : IEntityTypeConfiguration<ReimbursementClaim>
{
    public void Configure(EntityTypeBuilder<ReimbursementClaim> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Amount).HasColumnType("decimal(18,2)");
        builder.Property(c => c.Currency).IsRequired().HasMaxLength(3);
        builder.Property(c => c.IdempotencyKey).IsRequired().HasMaxLength(100);
        builder.Property(c => c.ExternalClaimId).HasMaxLength(100);
        builder.Property(c => c.FailureReason).HasMaxLength(500);

        builder.HasOne(c => c.RefillRequest)
            .WithOne(r => r.ReimbursementClaim)
            .HasForeignKey<ReimbursementClaim>(c => c.RefillRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Employee)
            .WithMany(e => e.Claims)
            .HasForeignKey(c => c.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.IdempotencyKey).IsUnique();
        builder.HasIndex(c => c.RefillRequestId).IsUnique();
    }
}

public class BudgetReservationConfiguration : IEntityTypeConfiguration<BudgetReservation>
{
    public void Configure(EntityTypeBuilder<BudgetReservation> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Amount).HasColumnType("decimal(18,2)");
        builder.Property(b => b.Currency).IsRequired().HasMaxLength(3);
        builder.Property(b => b.BudgetMonth).IsRequired().HasMaxLength(7);

        builder.HasOne(b => b.RefillRequest)
            .WithOne(r => r.BudgetReservation)
            .HasForeignKey<BudgetReservation>(b => b.RefillRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Office)
            .WithMany(o => o.BudgetReservations)
            .HasForeignKey(b => b.OfficeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(b => b.RefillRequestId).IsUnique();
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Type).IsRequired().HasMaxLength(100);
        builder.Property(n => n.Message).IsRequired().HasMaxLength(1000);

        builder.HasOne(n => n.RecipientEmployee)
            .WithMany()
            .HasForeignKey(n => n.RecipientEmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(n => n.RefillRequest)
            .WithMany(r => r.Notifications)
            .HasForeignKey(n => n.RefillRequestId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class ExchangeRateSnapshotConfiguration : IEntityTypeConfiguration<ExchangeRateSnapshot>
{
    public void Configure(EntityTypeBuilder<ExchangeRateSnapshot> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Rate).HasColumnType("decimal(18,6)");
        builder.Property(e => e.FromCurrency).IsRequired().HasMaxLength(3);
        builder.Property(e => e.ToCurrency).IsRequired().HasMaxLength(3);
        builder.Property(e => e.ReportMonth).IsRequired().HasMaxLength(7);
        builder.Property(e => e.Label).IsRequired().HasMaxLength(100);
    }
}
