namespace BanchoNET.Core.Models.Stable.Multiplayer;

/// <summary>
/// A one-shot deadline for a phase of a match that a client can leave hanging. Arming replaces any
/// pending deadline, and the timer fires once.
/// </summary>
public sealed class MatchTimeout : IDisposable
{
    private readonly Lock _lock = new();
    private Timer? _timer;

    public bool IsArmed
    {
        get
        {
            lock (_lock) return _timer != null;
        }
    }

    public void Arm(
        TimeSpan after,
        Func<Task> onElapsed
    ) {
        lock (_lock)
        {
            _timer?.Dispose();
            _timer = new Timer(_ => onElapsed(), null, after, Timeout.InfiniteTimeSpan);
        }
    }

    /// <returns>Whether the deadline was still outstanding, so only one caller acts on it.</returns>
    public bool Disarm()
    {
        lock (_lock)
        {
            if (_timer == null) return false;

            _timer.Dispose();
            _timer = null;

            return true;
        }
    }

    public void Dispose() => Disarm();
}