namespace SnackCounter.Core.Interfaces;

public interface IClock
{
    DateTime UtcNow { get; }
}
