using System.Diagnostics;

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
                DisposeQuietly(item);
                return;
            }
            var replaced = _pending;
            _pending = item;
            Monitor.Pulse(_gate);
            DisposeQuietly(replaced);
        }
    }

    public void DiscardPending()
    {
        lock (_gate)
        {
            var discarded = _pending;
            _pending = null;
            DisposeQuietly(discarded);
        }
    }

    // A throwing item must never leave a disposed item pending or skip shutdown's worker join.
    private static void DisposeQuietly(T? item)
    {
        try
        {
            item?.Dispose();
        }
        catch (Exception ex)
        {
            Trace.TraceError("LatestCaptureWorker item disposal threw: {0}", ex);
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
            try
            {
                try
                {
                    _process(item);
                }
                catch (Exception ex)
                {
                    try
                    {
                        _failed(item, ex);
                    }
                    catch (Exception callbackEx)
                    {
                        Trace.TraceError("LatestCaptureWorker failure callback threw: {0}", callbackEx);
                    }
                }
            }
            finally
            {
                DisposeQuietly(item);
            }
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _stopped = true;
            var discarded = _pending;
            _pending = null;
            Monitor.Pulse(_gate);
            DisposeQuietly(discarded);
        }
        // The worker never waits for the UI dispatcher; the OCR owner may now safely dispose it.
        _worker.GetAwaiter().GetResult();
    }
}
