using SnackCounter.Core.Interfaces;

namespace SnackCounter.Infrastructure;

public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

public class TestClock(DateTime fixedTime) : IClock
{
    public DateTime UtcNow { get; set; } = fixedTime;
}
