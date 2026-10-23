namespace Sitrep.Desktop;

internal interface IProcessIdentity : IDisposable
{
    string Name { get; }
    bool IsAlive { get; }
}

/// <summary>A name is reusable only while its owned process handle still identifies a live process.</summary>
internal sealed class ProcessIdentityCache(Func<uint, IProcessIdentity?> open, Func<long> milliseconds) : IDisposable
{
    private const int MaxThrottledPids = 64;
    private readonly object _gate = new();
    private readonly Dictionary<uint, long> _retryAfter = [];
    private uint _pid;
    private IProcessIdentity? _identity;

    public string GetName(uint pid)
    {
        lock (_gate)
        {
            if (pid == 0) { return string.Empty; }
            if (_pid == pid && _identity is not null)
            {
                if (_identity.IsAlive) { return _identity.Name; }
                _identity.Dispose();
                _identity = null;
                _pid = 0;
            }
            long now = milliseconds();
            // Failed lookups are throttled per PID, so alternating windows cannot reopen at input rate
            // and a failed PID never returns another process's name.
            if (_retryAfter.TryGetValue(pid, out long retryAfter) && now < retryAfter)
            {
                return string.Empty;
            }
            var identity = open(pid);
            if (identity is null || !identity.IsAlive)
            {
                // Exited between open and check, or liveness cannot be confirmed on the granted handle:
                // treat like a denied lookup so the throttle applies instead of reopening at input rate.
                identity?.Dispose();
                Throttle(pid, now + 1000, now);
                return string.Empty;
            }
            _retryAfter.Remove(pid);
            _identity?.Dispose();
            _identity = identity;
            _pid = pid;
            return identity.Name;
        }
    }

    private void Throttle(uint pid, long retryAfter, long now)
    {
        if (_retryAfter.Count >= MaxThrottledPids && !_retryAfter.ContainsKey(pid))
        {
            foreach (var expired in _retryAfter.Where(entry => entry.Value <= now).Select(entry => entry.Key).ToList())
            {
                _retryAfter.Remove(expired);
            }
            if (_retryAfter.Count >= MaxThrottledPids)
            {
                _retryAfter.Remove(_retryAfter.MinBy(entry => entry.Value).Key);
            }
        }
        _retryAfter[pid] = retryAfter;
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _identity?.Dispose();
            _identity = null;
            _pid = 0;
            _retryAfter.Clear();
        }
    }
}
