namespace BanchoNET.BeatmapServer.Services;

/// <summary>
/// Allows at most <c>limit</c> events within any rolling <c>window</c>.
/// </summary>
public sealed class SlidingWindowThrottler(
    int limit,
    TimeSpan window
) {
    private readonly Queue<DateTimeOffset> _eventTimes = new();
    private readonly Lock _sync = new();

    /// <summary>
    /// Registers an event if the limit allows it right now.
    /// </summary>
    public bool TryRegister() {
        lock (_sync)
        {
            var now = DateTimeOffset.UtcNow;
            PruneOldEntries(now);

            if (_eventTimes.Count >= limit)
                return false;

            _eventTimes.Enqueue(now);
            return true;
        }
    }

    /// <summary>
    /// Waits until the limit allows another event, then registers it.
    /// </summary>
    public async Task WaitAsync(
        CancellationToken cancellationToken = default
    ) {
        while (true)
        {
            TimeSpan delay;

            lock (_sync)
            {
                var now = DateTimeOffset.UtcNow;
                PruneOldEntries(now);

                if (_eventTimes.Count < limit)
                {
                    _eventTimes.Enqueue(now);
                    return;
                }

                delay = _eventTimes.Peek() + window - now;
            }

            await Task.Delay(delay, cancellationToken);
        }
    }

    private void PruneOldEntries(
        DateTimeOffset now
    ) {
        while (_eventTimes.Count > 0
               && now - _eventTimes.Peek() >= window)
        {
            _eventTimes.Dequeue();
        }
    }
}