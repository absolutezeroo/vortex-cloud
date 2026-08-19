using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Primitives.Console;
using Xunit;

namespace Vortex.Hosting.Tests;

/// <summary>
/// The ring buffer behind both consoles: the emulator's, where a logger provider feeds it, and the
/// supervisor's, where the child process's stdout does. It is the same structure either side, which
/// is why it lives in Primitives rather than being written twice.
/// </summary>
public sealed class ServerConsoleFeedTests
{
    [Fact]
    public void AViewerArrivingLate_StillSeesWhatItMissed()
    {
        ServerConsoleFeed buffer = new(10);
        buffer.Publish("first");
        buffer.Publish("second");

        using ServerConsoleSubscription subscription = buffer.Subscribe();

        subscription.Backlog.Should().Equal("first", "second");
    }

    [Fact]
    public void TheBuffer_DropsItsOldestLineOnceFull()
    {
        ServerConsoleFeed buffer = new(2);
        buffer.Publish("a");
        buffer.Publish("b");
        buffer.Publish("c");

        using ServerConsoleSubscription subscription = buffer.Subscribe();

        subscription.Backlog.Should().Equal("b", "c");
    }

    [Fact]
    public async Task LinesPublishedAfterSubscribing_ReachTheViewer()
    {
        ServerConsoleFeed buffer = new(10);
        using ServerConsoleSubscription subscription = buffer.Subscribe();

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
        ServerConsoleFeed buffer = new(10);
        buffer.Publish("\u001b[32minfo\u001b[0m: room ready");

        using ServerConsoleSubscription subscription = buffer.Subscribe();

        subscription.Backlog.Should().Equal("info: room ready");
    }

    /// <summary>
    /// A browser tab that stops reading must not become back-pressure on the emulator's stdout, so a
    /// full viewer channel drops its oldest line rather than blocking the publisher.
    /// </summary>
    [Fact]
    public void AViewerThatStopsReading_NeitherBlocksNorBreaksThePublisher()
    {
        ServerConsoleFeed buffer = new(2);
        using ServerConsoleSubscription stalled = buffer.Subscribe();

        for (int i = 0; i < 500; i++)
        {
            buffer.Publish($"line {i}");
        }

        // The publisher got through; the stalled viewer simply lost the middle.
        using ServerConsoleSubscription fresh = buffer.Subscribe();
        fresh.Backlog.Should().Equal("line 498", "line 499");
    }

    [Fact]
    public void ADetachedViewer_StopsReceiving()
    {
        ServerConsoleFeed buffer = new(10);
        ServerConsoleSubscription subscription = buffer.Subscribe();
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
        ServerConsoleFeed buffer = new(10);
        ServerConsoleSubscription subscription = buffer.Subscribe();

        subscription.Dispose();
        subscription.Dispose();

        buffer.Publish("still fine");
    }
}
