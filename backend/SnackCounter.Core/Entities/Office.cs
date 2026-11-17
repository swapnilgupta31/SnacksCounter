namespace SnackCounter.Core.Entities;

public class Office
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string TimeZoneId { get; set; } = string.Empty;
    public decimal MonthlyBudget { get; set; }

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    public ICollection<SnackBox> SnackBoxes { get; set; } = new List<SnackBox>();
    public ICollection<BudgetReservation> BudgetReservations { get; set; } = new List<BudgetReservation>();
}
