using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Console;
using Vortex.Supervisor.Configuration;
using Vortex.Supervisor.Process;
using Xunit;

namespace Vortex.Supervisor.Tests;

/// <summary>
/// Lifecycle, not plumbing. Every test here is about the order two things happen in — a restart
/// racing the previous process's exit notification, or two operators clicking at once — because that
/// is what a supervisor gets wrong, and getting it wrong means an emulator nobody can stop.
/// </summary>
public sealed class EmulatorProcessTests
{
    [Fact]
    public async Task Start_LeavesTheEmulatorRunning()
    {
        (EmulatorProcess emulator, FakeChildProcessFactory factory, _) = Supervisor();

        await emulator.StartAsync(CancellationToken.None);

        emulator.State.Should().Be(EmulatorState.Running);
        emulator.ProcessId.Should().Be(factory.Created[0].Id);
    }

    [Fact]
    public async Task Start_IsIgnoredWhenAlreadyRunning()
    {
        (EmulatorProcess emulator, FakeChildProcessFactory factory, _) = Supervisor();

        await emulator.StartAsync(CancellationToken.None);
        await emulator.StartAsync(CancellationToken.None);

        factory.Created.Should().ContainSingle("a second start must not spawn a second emulator");
    }

    [Fact]
    public async Task Stop_AsksPolitelyBeforeKilling()
    {
        (EmulatorProcess emulator, FakeChildProcessFactory factory, _) = Supervisor();

        await emulator.StartAsync(CancellationToken.None);
        await emulator.StopAsync(CancellationToken.None);

        FakeChildProcess process = factory.Created[0];
        process.Input.Should().ContainSingle().Which.Should().Be("quit");
        process.WasKilled.Should().BeFalse();
        emulator.State.Should().Be(EmulatorState.Stopped);
    }

    [Fact]
    public async Task Stop_KillsAProcessThatIgnoresTheShutdownCommand()
    {
        (EmulatorProcess emulator, FakeChildProcessFactory factory, _) = Supervisor(
            shutdownTimeoutSeconds: 1
        );

        await emulator.StartAsync(CancellationToken.None);
        factory.Created[0].ObeysShutdown = false;

        await emulator.StopAsync(CancellationToken.None);

        factory.Created[0].WasKilled.Should().BeTrue();
        emulator.State.Should().Be(EmulatorState.Stopped);
    }

    /// <summary>
    /// The bug this abstraction exists for. <c>Process.Exited</c> fires on a thread pool thread and
    /// can easily arrive after a restart has already launched the replacement. An exit handler that
    /// does not check which process it is hearing about will mark the running emulator as stopped
    /// and drop its handle — leaving a live hotel that the panel believes is down and can no longer
    /// stop, restart or send a command to.
    /// </summary>
    [Fact]
    public async Task Restart_SurvivesTheOldProcessReportingItsExitLate()
    {
        (EmulatorProcess emulator, FakeChildProcessFactory factory, _) = Supervisor();

        await emulator.StartAsync(CancellationToken.None);
        await emulator.RestartAsync(CancellationToken.None);

        factory.Created.Should().HaveCount(2);
        FakeChildProcess replacement = factory.Created[1];

        // Only now does the first process get around to announcing it died.
        factory.Created[0].AnnounceExit();

        emulator.State.Should().Be(EmulatorState.Running);
        emulator.ProcessId.Should().Be(replacement.Id);
    }

    [Fact]
    public async Task Restart_StillListensToTheProcessItActuallyStarted()
    {
        (EmulatorProcess emulator, FakeChildProcessFactory factory, _) = Supervisor();

        await emulator.StartAsync(CancellationToken.None);
        await emulator.RestartAsync(CancellationToken.None);

        // The guard must ignore stale generations without going deaf to the current one.
        factory.Created[1].ExitSilently(1);
        factory.Created[1].AnnounceExit();

        emulator.State.Should().Be(EmulatorState.Stopped);
        emulator.ProcessId.Should().BeNull();
    }

    [Fact]
    public async Task AnUnexpectedExit_IsReflectedInTheState()
    {
        (EmulatorProcess emulator, FakeChildProcessFactory factory, ServerConsoleFeed console) =
            Supervisor();

        await emulator.StartAsync(CancellationToken.None);

        factory.Created[0].ExitSilently(1);
        factory.Created[0].AnnounceExit();

        emulator.State.Should().Be(EmulatorState.Stopped);

        using ServerConsoleSubscription subscription = console.Subscribe();
        subscription.Backlog.Should().Contain(line => line.Contains("exited (code 1)"));
    }

    /// <summary>
    /// Two operators clicking restart at the same moment must not interleave into "stop, stop,
    /// start, start" — which ends with one emulator running and one orphan, or none at all.
    /// </summary>
    [Fact]
    public async Task ConcurrentRestarts_DoNotInterleave()
    {
        (EmulatorProcess emulator, FakeChildProcessFactory factory, _) = Supervisor();

        await emulator.StartAsync(CancellationToken.None);

        await Task.WhenAll(
            emulator.RestartAsync(CancellationToken.None),
            emulator.RestartAsync(CancellationToken.None),
            emulator.RestartAsync(CancellationToken.None)
        );

        emulator.State.Should().Be(EmulatorState.Running);
        factory.Created.Should().HaveCount(4, "one initial start plus one per restart");

        // Every process but the last must have been asked to shut down and then released.
        for (int i = 0; i < factory.Created.Count - 1; i++)
        {
            factory.Created[i].Input.Should().Contain("quit");
            factory.Created[i].WasDisposed.Should().BeTrue();
        }

        emulator.ProcessId.Should().Be(factory.Created[^1].Id);
    }

    [Fact]
    public async Task SendInput_IsRefusedWhileStopped()
    {
        (EmulatorProcess emulator, _, _) = Supervisor();

        bool sent = await emulator.SendInputAsync("help", CancellationToken.None);

        sent.Should().BeFalse();
    }

    [Fact]
    public async Task SendInput_ReachesTheRunningProcess()
    {
        (EmulatorProcess emulator, FakeChildProcessFactory factory, _) = Supervisor();

        await emulator.StartAsync(CancellationToken.None);
        bool sent = await emulator.SendInputAsync("help", CancellationToken.None);

        sent.Should().BeTrue();
        factory.Created[0].Input.Should().Contain("help");
    }

    [Fact]
    public async Task Stop_OnAStoppedEmulator_IsHarmless()
    {
        (EmulatorProcess emulator, FakeChildProcessFactory factory, _) = Supervisor();

        await emulator.StopAsync(CancellationToken.None);

        emulator.State.Should().Be(EmulatorState.Stopped);
        factory.Created.Should().BeEmpty();
    }

    [Fact]
    public async Task TheChildsOutput_ReachesTheServerConsoleFeed()
    {
        (EmulatorProcess emulator, FakeChildProcessFactory factory, ServerConsoleFeed console) =
            Supervisor();

        await emulator.StartAsync(CancellationToken.None);
        factory.Created[0].Output!("Room 42 loaded");

        using ServerConsoleSubscription subscription = console.Subscribe();
        subscription.Backlog.Should().Contain("Room 42 loaded");
    }

    /// <summary>
    /// The configured path used to be resolved with a bare <c>Path.GetFullPath</c>, i.e. against the
    /// current directory — so launching the supervisor from its own project folder rather than the
    /// repository root sent it looking for the emulator four levels above the wrong place, and the
    /// error named a directory nobody had configured.
    /// </summary>
    [Fact]
    public void ARelativeWorkingDirectory_ResolvesAgainstTheSupervisorNotTheCurrentDirectory()
    {
        string resolved = EmulatorProcess.ResolveWorkingDirectory("../sibling");

        resolved
            .Should()
            .Be(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../sibling")));
    }

    [Fact]
    public void AnAbsoluteWorkingDirectory_IsUsedAsGiven()
    {
        string absolute = Path.GetFullPath(AppContext.BaseDirectory);

        EmulatorProcess.ResolveWorkingDirectory(absolute).Should().Be(Path.GetFullPath(absolute));
    }

    [Fact]
    public void TheResolvedPath_IsAlwaysRooted() =>
        Path.IsPathRooted(EmulatorProcess.ResolveWorkingDirectory(".")).Should().BeTrue();

    private static (
        EmulatorProcess Emulator,
        FakeChildProcessFactory Factory,
        ServerConsoleFeed Console
    ) Supervisor(int shutdownTimeoutSeconds = 30)
    {
        SupervisorConfig config = new()
        {
            Emulator = new EmulatorProcessConfig
            {
                // The start path refuses a working directory that does not exist; the test
                // assembly's own directory always does.
                WorkingDirectory = ".",
                GracefulShutdownCommand = "quit",
                GracefulShutdownTimeoutSeconds = shutdownTimeoutSeconds,
            },
        };

        FakeChildProcessFactory factory = new();
        ServerConsoleFeed console = new(200);

        return (
            new EmulatorProcess(
                Options.Create(config),
                factory,
                console,
                NullLogger<EmulatorProcess>.Instance
            ),
            factory,
            console
        );
    }
}
