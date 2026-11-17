using SnackCounter.Core.Enums;

namespace SnackCounter.Core.Entities;

public class SnackBox
{
    public int Id { get; set; }
    public int OfficeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public SnackAvailability AvailabilityStatus { get; set; } = SnackAvailability.Available;
    public DateTime? LastRefilledAt { get; set; }

    public Office Office { get; set; } = null!;
    public ICollection<RefillRequest> RefillRequests { get; set; } = new List<RefillRequest>();
}
