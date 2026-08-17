using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Events;
using Vortex.Rooms.Grains.Systems;
using Vortex.Rooms.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Games;

/// <summary>
/// Locks the seam every room game plugs into. The point of it is that nothing which starts or stops a
/// round — the game-timer furni, the wired control-clock action, the tick loop, the avatar-left path —
/// names an individual game: they all go through <see cref="RoomGameSystem"/>, which fans out to whatever
/// is registered. Freeze was hardcoded at each of those call sites and a wired clock start reached only
/// half of them; these tests are what stops the next game repeating that.
/// </summary>
public sealed class RoomMinigameCoordinationTests
{
    [Fact]
    public async Task Freeze_Is_Registered_On_Every_Room()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);

        // A game that exists but was never plugged in is invisible: it builds, it tests, and it never
        // runs. This is the check that a room actually hosts the games written for it.
        harness
            .Grain.GameSystem.Minigames.Select(game => game.Name)
            .Should()
            .Contain(harness.Grain.FreezeSystem.Name);
    }

    [Fact]
    public async Task Starting_The_Round_Starts_Every_Registered_Game()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        RecordingMinigame first = new("first");
        RecordingMinigame second = new("second");
        harness.Grain.GameSystem.Register(first);
        harness.Grain.GameSystem.Register(second);

        await harness.Grain.GameSystem.StartGameAsync(CancellationToken.None).ConfigureAwait(true);

        first.Starts.Should().Be(1);
        second.Starts.Should().Be(1);
    }

    [Fact]
    public async Task The_Round_Is_Announced_Before_The_Games_Are_Started()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        RecordingMinigame game = new("first") { RoomEventCount = () => harness.RoomEvents.Count };
        harness.Grain.GameSystem.Register(game);

        await harness.Grain.GameSystem.StartGameAsync(CancellationToken.None).ConfigureAwait(true);

        // A GAME_STARTS box wired to a give-score action has to have run before a game clears anything:
        // the ordering is what keeps its points from vanishing without a trace.
        harness
            .RoomEvents.Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<WiredGameStartedEvent>();
        game.RoomEventsSeenAtStart.Should().Be(1);
    }

    [Fact]
    public async Task Starting_An_Already_Running_Round_Starts_Nothing_Twice()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        RecordingMinigame game = new("first");
        harness.Grain.GameSystem.Register(game);

        await harness.Grain.GameSystem.StartGameAsync(CancellationToken.None).ConfigureAwait(true);
        await harness.Grain.GameSystem.StartGameAsync(CancellationToken.None).ConfigureAwait(true);

        game.Starts.Should().Be(1);
    }

    [Fact]
    public async Task Ending_The_Round_Ends_Every_Registered_Game_After_Announcing_It()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        RecordingMinigame first = new("first");
        RecordingMinigame second = new("second");
        harness.Grain.GameSystem.Register(first);
        harness.Grain.GameSystem.Register(second);
        await harness.Grain.GameSystem.StartGameAsync(CancellationToken.None).ConfigureAwait(true);

        await harness.Grain.GameSystem.EndGameAsync(CancellationToken.None).ConfigureAwait(true);

        first.Ends.Should().Be(1);
        second.Ends.Should().Be(1);
        harness.RoomEvents.OfType<WiredGameEndedEvent>().Should().ContainSingle();
    }

    [Fact]
    public async Task A_Game_That_Ends_The_Round_Itself_Does_Not_Loop()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        RoomGameSystem games = harness.Grain.GameSystem;

        // Freeze does exactly this: the moment one team is left standing it calls the coordinator from
        // inside its own tick, and the coordinator then calls it back to wind the round down.
        RecordingMinigame game = new("first") { OnEnd = ct => games.EndGameAsync(ct) };
        games.Register(game);
        await games.StartGameAsync(CancellationToken.None).ConfigureAwait(true);

        await games.EndGameAsync(CancellationToken.None).ConfigureAwait(true);

        game.Ends.Should().Be(1);
        harness.RoomEvents.OfType<WiredGameEndedEvent>().Should().ContainSingle();
    }

    [Fact]
    public async Task A_Game_That_Throws_On_Start_Does_Not_Stop_The_Others()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        RecordingMinigame broken = new("broken")
        {
            OnStart = _ => throw new InvalidOperationException("no config"),
        };
        RecordingMinigame healthy = new("healthy");
        harness.Grain.GameSystem.Register(broken);
        harness.Grain.GameSystem.Register(healthy);

        // Games in a room are independent. A Freeze arena that cannot read its balance config must not
        // be able to stop the room's football match from kicking off — and must not throw the failure
        // back at whoever pressed the timer button.
        await harness.Grain.GameSystem.StartGameAsync(CancellationToken.None).ConfigureAwait(true);

        healthy.Starts.Should().Be(1);
    }

    [Fact]
    public async Task A_Game_That_Throws_On_End_Does_Not_Stop_The_Others()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        RecordingMinigame broken = new("broken")
        {
            OnEnd = _ => throw new InvalidOperationException("boom"),
        };
        RecordingMinigame healthy = new("healthy");
        harness.Grain.GameSystem.Register(broken);
        harness.Grain.GameSystem.Register(healthy);
        await harness.Grain.GameSystem.StartGameAsync(CancellationToken.None).ConfigureAwait(true);

        await harness.Grain.GameSystem.EndGameAsync(CancellationToken.None).ConfigureAwait(true);

        // A round left half-ended is worse than one that ended noisily: the survivors must wind down.
        healthy.Ends.Should().Be(1);
    }

    [Fact]
    public async Task A_Player_Leaving_Reaches_Every_Registered_Game()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        RecordingMinigame first = new("first");
        RecordingMinigame second = new("second");
        harness.Grain.GameSystem.Register(first);
        harness.Grain.GameSystem.Register(second);

        await harness
            .Grain.GameSystem.OnPlayerLeftAsync(RoomHarness.Stranger, CancellationToken.None)
            .ConfigureAwait(true);

        first.PlayersLeft.Should().ContainSingle().Which.Should().Be(RoomHarness.Stranger);
        second.PlayersLeft.Should().ContainSingle().Which.Should().Be(RoomHarness.Stranger);
    }

    /// <summary>A stand-in game that counts what the coordinator did to it.</summary>
    private sealed class RecordingMinigame(string name) : IRoomMinigame
    {
        public string Name { get; } = name;

        public int Starts { get; private set; }

        public int Ends { get; private set; }

        public int Ticks { get; private set; }

        public List<PlayerId> PlayersLeft { get; } = [];

        /// <summary>How many room events had been published by the time this game was started — the way
        /// to observe that the round was announced first.</summary>
        public int RoomEventsSeenAtStart { get; private set; }

        /// <summary>Reads the room's published-event count, so the game can see what had already fired
        /// when it was started.</summary>
        public Func<int>? RoomEventCount { get; init; }

        /// <summary>Extra behaviour to run on start, for the failure-isolation case.</summary>
        public Func<CancellationToken, Task>? OnStart { get; init; }

        /// <summary>Extra behaviour to run on end, for the re-entrancy and failure-isolation cases.</summary>
        public Func<CancellationToken, Task>? OnEnd { get; init; }

        public Task StartAsync(CancellationToken ct)
        {
            Starts++;
            RoomEventsSeenAtStart = RoomEventCount?.Invoke() ?? 0;

            return OnStart?.Invoke(ct) ?? Task.CompletedTask;
        }

        public Task EndAsync(CancellationToken ct)
        {
            Ends++;

            return OnEnd?.Invoke(ct) ?? Task.CompletedTask;
        }

        public Task TickAsync(long nowMs, CancellationToken ct)
        {
            Ticks++;

            return Task.CompletedTask;
        }

        public Task OnPlayerLeftAsync(PlayerId playerId, CancellationToken ct)
        {
            PlayersLeft.Add(playerId);

            return Task.CompletedTask;
        }
    }
}
