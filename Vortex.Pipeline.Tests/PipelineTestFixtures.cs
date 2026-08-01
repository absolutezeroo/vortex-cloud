using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Pipeline.Attributes;
using Vortex.Pipeline.Registry;

namespace Vortex.Pipeline.Tests;

// A small, self-contained envelope/context/meta shape for exercising EnvelopeHost<,,> directly --
// deliberately NOT any real Vortex message/event type, per the TEST-01 brief.

public class TestEnvelope
{
    public string Payload { get; init; } = string.Empty;
}

public sealed class DerivedTestEnvelope : TestEnvelope { }

public sealed class TestContext
{
    public bool ShortCircuit { get; set; }
}

/// <summary>Thread-safe call-order/invocation recorder shared (via DI) between the host and the
/// handler/behavior instances it activates for each publish.</summary>
public sealed class Recorder
{
    private readonly ConcurrentQueue<string> _entries = new();

    public IReadOnlyCollection<string> Entries => _entries;

    public void Record(string entry) => _entries.Enqueue(entry);
}

/// <summary>Tracks the maximum number of handlers observed running concurrently, to verify
/// <see cref="EnvelopeHostOptions{TEnvelope,TMeta,TContext}.MaxHandlerDegreeOfParallelism"/> is
/// actually enforced by the dispatch engine and not just accepted as a no-op setting.</summary>
public sealed class ConcurrencyTracker
{
    private int _current;
    private int _max;

    public int Max => Volatile.Read(ref _max);

    public void Enter()
    {
        int now = Interlocked.Increment(ref _current);

        int observedMax;
        do
        {
            observedMax = Volatile.Read(ref _max);
            if (now <= observedMax)
            {
                break;
            }
        } while (Interlocked.CompareExchange(ref _max, now, observedMax) != observedMax);
    }

    public void Exit() => Interlocked.Decrement(ref _current);
}

public sealed class RecordingHandlerA(Recorder recorder) : IHandler<TestEnvelope, TestContext>
{
    public ValueTask HandleAsync(TestEnvelope env, TestContext ctx, CancellationToken ct)
    {
        recorder.Record($"A:{env.Payload}");

        return ValueTask.CompletedTask;
    }
}

public sealed class RecordingHandlerB(Recorder recorder) : IHandler<TestEnvelope, TestContext>
{
    public ValueTask HandleAsync(TestEnvelope env, TestContext ctx, CancellationToken ct)
    {
        recorder.Record($"B:{env.Payload}");

        return ValueTask.CompletedTask;
    }
}

public sealed class ThrowingHandler : IHandler<TestEnvelope, TestContext>
{
    public sealed class BoomException(string message) : System.Exception(message);

    public ValueTask HandleAsync(TestEnvelope env, TestContext ctx, CancellationToken ct) =>
        throw new BoomException("handler blew up");
}

public sealed class DerivedOnlyHandler(Recorder recorder)
    : IHandler<DerivedTestEnvelope, TestContext>
{
    public ValueTask HandleAsync(DerivedTestEnvelope env, TestContext ctx, CancellationToken ct)
    {
        recorder.Record($"DerivedOnly:{env.Payload}");

        return ValueTask.CompletedTask;
    }
}

public sealed class ConcurrencyTrackingHandler(ConcurrencyTracker tracker)
    : IHandler<TestEnvelope, TestContext>
{
    public async ValueTask HandleAsync(TestEnvelope env, TestContext ctx, CancellationToken ct)
    {
        tracker.Enter();
        try
        {
            await Task.Delay(30, ct).ConfigureAwait(true);
        }
        finally
        {
            tracker.Exit();
        }
    }
}

[Order(10)]
public sealed class LowPriorityBehavior(Recorder recorder) : IBehavior<TestEnvelope, TestContext>
{
    public async ValueTask InvokeAsync(
        TestEnvelope env,
        TestContext ctx,
        System.Func<ValueTask> next,
        CancellationToken ct
    )
    {
        recorder.Record("Behavior(10):before");
        await next().ConfigureAwait(true);
        recorder.Record("Behavior(10):after");
    }
}

[Order(-5)]
public sealed class HighPriorityBehavior(Recorder recorder) : IBehavior<TestEnvelope, TestContext>
{
    public async ValueTask InvokeAsync(
        TestEnvelope env,
        TestContext ctx,
        System.Func<ValueTask> next,
        CancellationToken ct
    )
    {
        recorder.Record("Behavior(-5):before");
        await next().ConfigureAwait(true);
        recorder.Record("Behavior(-5):after");
    }
}

/// <summary>A behavior with no [Order] attribute -- exercises the "defaults to 0" fallback that
/// <c>EnvelopeFeatureProcessor</c> applies via <c>GetCustomAttribute&lt;OrderAttribute&gt;()?.Value ?? 0</c>.</summary>
public sealed class UnorderedBehavior(Recorder recorder) : IBehavior<TestEnvelope, TestContext>
{
    public async ValueTask InvokeAsync(
        TestEnvelope env,
        TestContext ctx,
        System.Func<ValueTask> next,
        CancellationToken ct
    )
    {
        recorder.Record("Behavior(0):before");
        await next().ConfigureAwait(true);
        recorder.Record("Behavior(0):after");
    }
}
