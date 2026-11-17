namespace SnackCounter.Core.Entities;

public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int HomeOfficeId { get; set; }
    public int CurrentOfficeId { get; set; }
    public string PayrollCurrency { get; set; } = string.Empty;
    public bool IsVolunteer { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsOnLeave { get; set; }
    public bool IsPresentToday { get; set; }
    public string Role { get; set; } = "Employee"; // Employee, Volunteer, Admin
    public DateTime? LastAssignedAt { get; set; }

    public Office HomeOffice { get; set; } = null!;
    public Office CurrentOffice { get; set; } = null!;
    public ICollection<RefillRequest> AssignedRefills { get; set; } = new List<RefillRequest>();
    public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
    public ICollection<ReimbursementClaim> Claims { get; set; } = new List<ReimbursementClaim>();
}
