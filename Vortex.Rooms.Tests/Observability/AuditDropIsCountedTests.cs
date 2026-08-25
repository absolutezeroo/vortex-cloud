using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vortex.Observability.Audit;
using Vortex.Observability.Configuration;
using Vortex.Observability.Diagnostics;
using Vortex.Primitives.Observability;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Observability;

/// <summary>
/// An audit event that never reaches the table is counted, not merely logged.
/// </summary>
/// <remarks>
/// The control-plane note calls an alert on this one priority-one, and the reason is worth stating:
/// every other counter in the system is about how well something is running, and this one is about
/// whether a privileged mutation is accounted for at all. Before it, a saturated channel dropped the
/// event with a warning — which is a signal only for as long as somebody is tailing the log, and the
/// moment it matters most (the writer cannot keep up, so the hotel is already busy or the database is
/// already unwell) is exactly the moment nobody is.
/// </remarks>
public sealed class AuditDropIsCountedTests
{
    [Fact]
    public void AnEventThatFitsInTheChannelCountsNothing()
    {
        List<string> stages = [];
        ChannelAuditSink sink = Sink(capacity: 4, stages);

        sink.Emit(Event("staff.mute"));

        stages.Should().BeEmpty();
    }

    [Fact]
    public void AnEventDroppedByASaturatedChannelIsCounted()
    {
        List<string> stages = [];

        // Capacity one and nothing reading: the second event has nowhere to go. This is the shape of
        // the real failure -- the writer stalled on the database and the queue filled behind it.
        ChannelAuditSink sink = Sink(capacity: 1, stages);

        sink.Emit(Event("staff.ban"));
        sink.Emit(Event("staff.credit"));

        stages.Should().Equal(["enqueue"]);
    }

    private static ChannelAuditSink Sink(int capacity, List<string> stages) =>
        new(
            new AuditChannel(
                Options.Create(new ObservabilityConfig { AuditChannelCapacity = capacity })
            ),
            FakeProxy.Create<IVortexContextAccessor>(_ => null),
            FakeProxy.Create<IVortexMetrics>(call =>
            {
                if (call.Method.Name == nameof(IVortexMetrics.AuditWriteFailed))
                {
                    stages.Add((string)call.Args![0]!);
                }

                return null;
            }),
            NullLogger<ChannelAuditSink>.Instance
        );

    private static AuditEvent Event(string action) =>
        new()
        {
            Category = AuditCategory.Staff,
            Action = action,
            Severity = AuditSeverity.Notice,
            Result = AuditResult.Success,
        };
}
