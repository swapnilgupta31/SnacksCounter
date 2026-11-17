using Microsoft.EntityFrameworkCore;
using SnackCounter.Core.Entities;
using SnackCounter.Core.Interfaces;
using SnackCounter.Infrastructure.Data;

namespace SnackCounter.Infrastructure.Integrations;

public class DbChatNotificationService(SnackCounterDbContext db) : IChatNotificationService
{
    public async Task NotifyAsync(int? recipientEmployeeId, int? refillRequestId, string type, string message)
    {
        db.Notifications.Add(new Notification
        {
            RecipientEmployeeId = recipientEmployeeId,
            RefillRequestId = refillRequestId,
            Type = type,
            Message = message,
            SentAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }
}
