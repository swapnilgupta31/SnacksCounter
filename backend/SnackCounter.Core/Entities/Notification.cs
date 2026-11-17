namespace SnackCounter.Core.Entities;

public class Notification
{
    public int Id { get; set; }
    public int? RecipientEmployeeId { get; set; }
    public int? RefillRequestId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public Employee? RecipientEmployee { get; set; }
    public RefillRequest? RefillRequest { get; set; }
}
