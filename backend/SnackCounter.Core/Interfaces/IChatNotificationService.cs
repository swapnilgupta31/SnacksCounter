namespace SnackCounter.Core.Interfaces;

public interface IChatNotificationService
{
    Task NotifyAsync(int? recipientEmployeeId, int? refillRequestId, string type, string message);
}
