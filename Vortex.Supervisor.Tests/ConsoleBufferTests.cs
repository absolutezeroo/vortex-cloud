using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Supervisor.Console;
using Xunit;

namespace Vortex.Supervisor.Tests;

public sealed class ConsoleBufferTests
{
    [Fact]
    public void AViewerArrivingLate_StillSeesWhatItMissed()
    {
        ConsoleBuffer buffer = new(10);
        buffer.Publish("first");
        buffer.Publish("second");

        using ConsoleSubscription subscription = buffer.Subscribe();

        subscription.Backlog.Should().Equal("first", "second");
    }

    [Fact]
    public void TheBuffer_DropsItsOldestLineOnceFull()
    {
        ConsoleBuffer buffer = new(2);
        buffer.Publish("a");
        buffer.Publish("b");
        buffer.Publish("c");

        using ConsoleSubscription subscription = buffer.Subscribe();

        subscription.Backlog.Should().Equal("b", "c");
    }

    [Fact]
    public async Task LinesPublishedAfterSubscribing_ReachTheViewer()
    {
        ConsoleBuffer buffer = new(10);
        using ConsoleSubscription subscription = buffer.Subscribe();

        buffer.Publish("live");

        string line = await subscription.Reader.ReadAsync(CancellationToken.None);
        line.Should().Be("live");
    }

    /// <summary>
    /// The emulator's console logger emits colour, which is noise once the destination is a browser.
    /// </summary>
    [Fact]
    public void AnsiColourCodes_AreStripped()
    {
        ConsoleBuffer buffer = new(10);
        buffer.Publish("\u001b[32minfo\u001b[0m: room ready");

        using ConsoleSubscription subscription = buffer.Subscribe();

        subscription.Backlog.Should().Equal("info: room ready");
    }

    /// <summary>
    /// A browser tab that stops reading must not become back-pressure on the emulator's stdout, so a
    /// full viewer channel drops its oldest line rather than blocking the publisher.
    /// </summary>
    [Fact]
    public void AViewerThatStopsReading_NeitherBlocksNorBreaksThePublisher()
    {
        ConsoleBuffer buffer = new(2);
        using ConsoleSubscription stalled = buffer.Subscribe();

        for (int i = 0; i < 500; i++)
        {
            buffer.Publish($"line {i}");
        }

        // The publisher got through; the stalled viewer simply lost the middle.
        using ConsoleSubscription fresh = buffer.Subscribe();
        fresh.Backlog.Should().Equal("line 498", "line 499");
    }

    [Fact]
    public void ADetachedViewer_StopsReceiving()
    {
        ConsoleBuffer buffer = new(10);
        ConsoleSubscription subscription = buffer.Subscribe();
        subscription.Dispose();

        buffer.Publish("after");

        List<string> received = [];

        while (subscription.Reader.TryRead(out string? line))
        {
            received.Add(line);
        }

        received.Should().BeEmpty();
    }

    [Fact]
    public void DisposingTwice_IsHarmless()
    {
        ConsoleBuffer buffer = new(10);
        ConsoleSubscription subscription = buffer.Subscribe();

        subscription.Dispose();
        subscription.Dispose();

        buffer.Publish("still fine");
    }
}
