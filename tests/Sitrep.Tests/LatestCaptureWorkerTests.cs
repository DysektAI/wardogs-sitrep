using System.Collections.Concurrent;
using Sitrep.Core;

namespace Sitrep.Tests;

public sealed class LatestCaptureWorkerTests
{
    private sealed class Frame(int id) : IDisposable
    {
        public int Id { get; } = id;
        public int DisposeCount;
        public TaskCompletionSource Disposed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void Dispose()
        {
            Interlocked.Increment(ref DisposeCount);
            Disposed.TrySetResult();
        }
    }

    [Fact]
    public async Task BusyWorkerKeepsOnlyNewestSnapshotAndDisposesEveryFrame()
    {
        using var release = new ManualResetEventSlim();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var seen = new ConcurrentQueue<int>();
        var errors = new ConcurrentQueue<Exception>();
        using var worker = new LatestCaptureWorker<Frame>(frame =>
        {
            seen.Enqueue(frame.Id);
            if (frame.Id == 1)
            {
                started.TrySetResult();
                if (!release.Wait(TimeSpan.FromSeconds(10))) { throw new TimeoutException(); }
            }
        }, (_, ex) => errors.Enqueue(ex));
        var first = new Frame(1);
        var replaced = new Frame(2);
        var newest = new Frame(3);
        try
        {
            worker.Enqueue(first);
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            worker.Enqueue(replaced);
            worker.Enqueue(newest);
            Assert.Equal(1, replaced.DisposeCount);
            release.Set();
            await newest.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(new[] { 1, 3 }, seen.ToArray());
            Assert.Empty(errors);
            Assert.Equal(1, first.DisposeCount);
            Assert.Equal(1, newest.DisposeCount);
        }
        finally { release.Set(); }
    }

    [Fact]
    public async Task RecognitionExceptionIsObservedAndWorkerProcessesNextFrame()
    {
        var failures = new ConcurrentQueue<int>();
        using var worker = new LatestCaptureWorker<Frame>(frame =>
        {
            if (frame.Id == 1) { throw new InvalidOperationException("synthetic OCR failure"); }
        }, (frame, _) => failures.Enqueue(frame.Id));
        var first = new Frame(1);
        worker.Enqueue(first);
        await first.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var second = new Frame(2);
        worker.Enqueue(second);
        await second.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(new[] { 1 }, failures.ToArray());
        Assert.Equal(1, first.DisposeCount);
        Assert.Equal(1, second.DisposeCount);
    }

    [Fact]
    public async Task ShutdownDiscardsQueuedFrameAndDrainsActiveBeforeEngineCanBeDisposed()
    {
        using var release = new ManualResetEventSlim();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var worker = new LatestCaptureWorker<Frame>(_ =>
        {
            started.TrySetResult();
            if (!release.Wait(TimeSpan.FromSeconds(10))) { throw new TimeoutException(); }
        }, (_, ex) => throw new InvalidOperationException("unexpected failure", ex));
        var active = new Frame(1);
        var queued = new Frame(2);
        try
        {
            worker.Enqueue(active);
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            worker.Enqueue(queued);
            var shutdown = Task.Run(worker.Dispose);
            await queued.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.False(shutdown.IsCompleted);
            Assert.Equal(0, active.DisposeCount);
            release.Set();
            await shutdown.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(1, active.DisposeCount);
            var late = new Frame(3);
            worker.Enqueue(late);
            Assert.Equal(1, late.DisposeCount);
        }
        finally { release.Set(); }
    }

    private sealed class FaultyFrame(int id, bool throwOnDispose = false) : IDisposable
    {
        public int Id { get; } = id;
        public int DisposeCount;
        public TaskCompletionSource Disposed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void Dispose()
        {
            Interlocked.Increment(ref DisposeCount);
            Disposed.TrySetResult();
            if (throwOnDispose)
            {
                throw new InvalidOperationException("synthetic dispose failure");
            }
        }
    }

    [Fact]
    public async Task FailureCallbackOrDisposalExceptionDoesNotTerminateWorker()
    {
        var processed = new ConcurrentQueue<int>();
        var callbackInvoked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var worker = new LatestCaptureWorker<FaultyFrame>(frame =>
        {
            if (frame.Id == 1)
            {
                throw new InvalidOperationException("synthetic process failure");
            }
            processed.Enqueue(frame.Id);
        }, (frame, _) =>
        {
            if (frame.Id == 1)
            {
                callbackInvoked.TrySetResult();
                throw new InvalidOperationException("synthetic failure-callback exception");
            }
        });

        var first = new FaultyFrame(1);
        worker.Enqueue(first);
        await callbackInvoked.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await first.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var second = new FaultyFrame(2, throwOnDispose: true);
        worker.Enqueue(second);
        await second.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var third = new FaultyFrame(3);
        worker.Enqueue(third);
        await third.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(new[] { 2, 3 }, processed.ToArray());
        Assert.Equal(1, first.DisposeCount);
        Assert.Equal(1, second.DisposeCount);
        Assert.Equal(1, third.DisposeCount);
    }
}
