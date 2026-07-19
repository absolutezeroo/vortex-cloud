using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Events.Registry;
using Vortex.Primitives.Events;
using Vortex.Rooms.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Events;

public sealed class EventSystemTests
{
    private const string CORRELATION_ID = "phase2-correlation";

    [Fact]
    public async Task PublishCancellableAsync_ReturnsCancelledContext_AndSkipsHandlers()
    {
        EventTestHarness harness = new EventTestHarness(CORRELATION_ID);
        CancellingGroupCreatingBehavior behavior = new CancellingGroupCreatingBehavior();
        CountingGroupCreatingHandler handler = new CountingGroupCreatingHandler();
        harness.RegisterBehavior<GroupCreatingEvent>(behavior);
        harness.RegisterHandler<GroupCreatingEvent>(handler);

        EventContext context = await harness
            .System.PublishCancellableAsync(
                new GroupCreatingEvent(1, "Blocked", 10, 10),
                CancellationToken.None
            )
            .ConfigureAwait(true);

        context.Cancel.Should().BeTrue();
        context.CancelReason.Should().Be("blocked");
        context.CorrelationId.Should().Be(CORRELATION_ID);
        behavior.CorrelationId.Should().Be(CORRELATION_ID);
        handler.Count.Should().Be(0);
    }

    [Fact]
    public async Task ThrowingBehavior_IsReported_AndDoesNotCancelOrSkipHandlers()
    {
        EventTestHarness harness = new EventTestHarness(CORRELATION_ID);
        CountingGroupCreatingHandler handler = new CountingGroupCreatingHandler();
        harness.RegisterBehavior<GroupCreatingEvent>(new ThrowingGroupCreatingBehavior());
        harness.RegisterHandler<GroupCreatingEvent>(handler);

        EventContext context = await harness
            .System.PublishCancellableAsync(
                new GroupCreatingEvent(1, "Allowed", 10, 10),
                CancellationToken.None
            )
            .ConfigureAwait(true);

        context.Cancel.Should().BeFalse();
        handler.Count.Should().Be(1);
        harness.ErrorSink.Count.Should().Be(1);
        harness.ErrorSink.LastSource.Should().Be("event-registry.behavior-invoke");
    }

    private sealed class CancellingGroupCreatingBehavior : IEventBehavior<GroupCreatingEvent>
    {
        public string? CorrelationId { get; private set; }

        public async ValueTask InvokeAsync(
            GroupCreatingEvent env,
            EventContext ctx,
            Func<ValueTask> next,
            CancellationToken ct
        )
        {
            CorrelationId = ctx.CorrelationId;
            ctx.Cancel = true;
            ctx.CancelReason = "blocked";
            await next().ConfigureAwait(false);
        }
    }

    private sealed class ThrowingGroupCreatingBehavior : IEventBehavior<GroupCreatingEvent>
    {
        public ValueTask InvokeAsync(
            GroupCreatingEvent env,
            EventContext ctx,
            Func<ValueTask> next,
            CancellationToken ct
        )
        {
            throw new InvalidOperationException("plugin failed");
        }
    }

    private sealed class CountingGroupCreatingHandler : IEventHandler<GroupCreatingEvent>
    {
        public int Count { get; private set; }

        public ValueTask HandleAsync(GroupCreatingEvent env, EventContext ctx, CancellationToken ct)
        {
            Count++;

            return ValueTask.CompletedTask;
        }
    }
}
