namespace Sitrep.Core;

/// <summary>Owns one active item and one replaceable pending item; drains before disposal returns.</summary>
public sealed class LatestCaptureWorker<T> : IDisposable where T : class, IDisposable
{
    private readonly object _gate = new();
    private readonly Action<T> _process;
    private readonly Action<T, Exception> _failed;
    private readonly Task _worker;
    private T? _pending;
    private bool _stopped;

    public LatestCaptureWorker(Action<T> process, Action<T, Exception> failed)
    {
        _process = process;
        _failed = failed;
        _worker = Task.Run(Run);
    }

    // Ownership transfers on every call, including calls after shutdown.
    public void Enqueue(T item)
    {
        lock (_gate)
        {
            if (_stopped)
            {
                item.Dispose();
                return;
            }
            _pending?.Dispose();
            _pending = item;
            Monitor.Pulse(_gate);
        }
    }

    public void DiscardPending()
    {
        lock (_gate)
        {
            _pending?.Dispose();
            _pending = null;
        }
    }

    private void Run()
    {
        while (true)
        {
            T item;
            lock (_gate)
            {
                while (!_stopped && _pending is null)
                {
                    Monitor.Wait(_gate);
                }
                if (_stopped)
                {
                    return;
                }
                item = _pending ?? throw new InvalidOperationException("Worker woke without a pending capture.");
                _pending = null;
            }
            using (item)
            {
                try
                {
                    _process(item);
                }
                catch (Exception ex)
                {
                    _failed(item, ex);
                }
            }
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _stopped = true;
            _pending?.Dispose();
            _pending = null;
            Monitor.Pulse(_gate);
        }
        // The worker never waits for the UI dispatcher; the OCR owner may now safely dispose it.
        _worker.GetAwaiter().GetResult();
    }
}
