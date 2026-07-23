using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Events;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;
using Vortex.Rooms.Wired;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor;

/// <summary>
/// The Habbo game timer / countdown counter furni (interaction <c>game_timer</c>: wf_game_upcounter*,
/// wf_upcounter*, fball_counter, bb_counter, …). It has two buttons, sent as the use <c>param</c>
/// (mirrors Arcturus <c>InteractionGameTimer</c> and the client <c>FurnitureCounterClockLogic</c>):
/// <list type="bullet">
/// <item><c>1</c> = start / stop: when stopped, start the countdown from the selected duration and
/// start the room game; when running, pause; when paused, resume.</item>
/// <item><c>2</c> = increase time: when stopped, cycle the selected duration up through the preset
/// steps (the furni's customParams).</item>
/// </list>
/// While running it counts DOWN one second at a time; reaching zero ends the room game. Only players
/// with room rights can operate it (enforced by the usage policy before the use reaches here).
/// </summary>
[RoomObjectLogic("game_timer")]
public class FurnitureGameTimerLogic(IStuffDataFactory stuffDataFactory, IRoomFloorItemContext ctx)
    : FurnitureFloorLogic(stuffDataFactory, ctx),
        IWiredCounter
{
    private const int ActionStartStop = 1;
    private const int ActionIncreaseTime = 2;

    // InteractionGameTimer's default steps, used when the furni carries no customParams.
    private static readonly int[] DefaultSteps = [30, 60, 120, 180, 300, 600];

    // The running countdown value (timeNow) is ephemeral — it dies with the room, like Arcturus's
    // RoomActive state — and is broadcast, never persisted.
    protected override StuffPersistanceType _stuffPersistanceType =>
        StuffPersistanceType.RoomActive;

    private readonly WiredCountdownClock _clock = new();
    private int[] _steps = DefaultSteps;
    private int _baseTime;
    private bool _gameActive; // Arcturus isRunning: a game has been started (it may be paused)
    private bool _initialized;

    /// <summary>The timer's current displayed value in whole seconds — read by the wired
    /// CLOCK_TIME_MATCHES condition.</summary>
    public int RemainingSeconds => _clock.RemainingSeconds;

    /// <summary>Only room controllers/owners may operate the timer (Arcturus room.hasRights).</summary>
    public override FurnitureUsageType GetUsagePolicy() => FurnitureUsageType.Controller;

    public override async Task OnAttachAsync(CancellationToken ct)
    {
        EnsureInitialized();
        ApplyDisplay();

        await base.OnAttachAsync(ct);
    }

    public override async Task OnUseAsync(ActionContext ctx, int param, CancellationToken ct)
    {
        EnsureInitialized();

        switch (param)
        {
            case ActionStartStop:
                await HandleStartStopAsync(ct);
                break;
            case ActionIncreaseTime:
                await HandleIncreaseTimeAsync(ct);
                break;
        }
    }

    /// <summary>Advance the countdown; called once per room tick by <see cref="Grains.Systems.RoomGameTimerSystem"/>.</summary>
    public async Task AdvanceAsync(long nowMs, CancellationToken ct)
    {
        if (!_gameActive)
        {
            return;
        }

        CountdownTick tick = _clock.Advance(nowMs);

        if (tick.DisplayChanged)
        {
            ApplyDisplay();

            // Let the wired CLOCK_REACH_TIME trigger react to the new remaining time.
            await _ctx.PublishRoomEventAsync(
                new GameTimerTimeReachedEvent
                {
                    RoomId = _ctx.RoomId,
                    CausedBy = ActionContext.Wired,
                    RemainingSeconds = _clock.RemainingSeconds,
                },
                ct
            );
        }

        if (tick.ReachedZero)
        {
            _gameActive = false;

            await _roomGrain.GameSystem.EndGameAsync(ct);
        }
    }

    private async Task HandleStartStopAsync(CancellationToken ct)
    {
        if (!_gameActive)
        {
            StartCountdown();

            await _roomGrain.GameSystem.StartGameAsync(ct);

            return;
        }

        if (_clock.IsRunning)
        {
            _clock.Halt(); // pause
        }
        else
        {
            _clock.Resume(); // unpause
        }
    }

    private void StartCountdown()
    {
        _gameActive = true;
        _clock.SetSeconds(_baseTime);
        _clock.Start();

        ApplyDisplay();
    }

    // --- IWiredCounter: the wired control_clock / adjust_clock actions drive the timer through these.
    // GAME_STARTS / GAME_ENDS are raised by the control_clock action itself, so these only touch the
    // clock/game state. ---

    void IWiredCounter.StartClock()
    {
        EnsureInitialized();

        if (!_gameActive)
        {
            StartCountdown();
        }
    }

    void IWiredCounter.HaltClock() => _clock.Halt();

    void IWiredCounter.ResumeClock() => _clock.Resume();

    void IWiredCounter.ResetClock()
    {
        EnsureInitialized();

        _gameActive = false;
        _clock.Halt();
        _clock.SetSeconds(_baseTime);

        ApplyDisplay();
    }

    void IWiredCounter.SetClockSeconds(int seconds)
    {
        _clock.SetSeconds(seconds);

        ApplyDisplay();
    }

    void IWiredCounter.AddClockSeconds(int seconds)
    {
        _clock.AddSeconds(seconds);

        ApplyDisplay();
    }

    private async Task HandleIncreaseTimeAsync(CancellationToken ct)
    {
        if (!_gameActive)
        {
            IncreaseTimer();

            return;
        }

        // Running games ignore the button; a paused game ends before the duration is bumped.
        if (!_clock.IsRunning)
        {
            _gameActive = false;

            await _roomGrain.GameSystem.EndGameAsync(ct);

            IncreaseTimer();
        }
    }

    /// <summary>Bump the selected duration up to the next preset step above the current value, wrapping
    /// to the first step past the top (mirrors Arcturus increaseTimer).</summary>
    private void IncreaseTimer()
    {
        int current = _clock.RemainingSeconds;
        int newBase;

        if (current != _baseTime)
        {
            newBase = _baseTime;
        }
        else
        {
            newBase = _steps[0];

            foreach (int step in _steps)
            {
                if (current < step)
                {
                    newBase = step;
                    break;
                }
            }
        }

        _baseTime = newBase;
        _clock.SetSeconds(newBase);

        PersistBaseTime();
        ApplyDisplay();
    }

    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        _steps = ParseSteps(_ctx.Definition.ExtraData);
        _baseTime = LoadBaseTime();
        _clock.SetSeconds(_baseTime);
    }

    /// <summary>Read the persisted selected duration (baseTime); falls back to the first preset step.
    /// This is the durable half of Arcturus's <c>timeNow\tbaseTime</c> — the running countdown is not
    /// persisted, but the chosen duration survives a room reload.</summary>
    private int LoadBaseTime()
    {
        if (
            _ctx.RoomObject.ExtraData.TryGetSection(
                ExtraDataSectionType.GAME_TIMER,
                out JsonElement element
            )
            && element.ValueKind == JsonValueKind.Number
            && element.TryGetInt32(out int stored)
            && stored > 0
        )
        {
            return stored;
        }

        return _steps[0];
    }

    private void PersistBaseTime()
    {
        _ctx.RoomObject.ExtraData.UpdateSection(ExtraDataSectionType.GAME_TIMER, _baseTime);
    }

    /// <summary>Mirror the current countdown value onto the furni state and broadcast it (ephemeral —
    /// no persist, no state-changed event).</summary>
    private void ApplyDisplay()
    {
        StuffData.SetState(_clock.RemainingSeconds.ToString(CultureInfo.InvariantCulture));

        _ = _ctx.RefreshStuffDataAsync();
    }

    private static int[] ParseSteps(string? customParams)
    {
        if (string.IsNullOrWhiteSpace(customParams))
        {
            return DefaultSteps;
        }

        List<int> steps = [];

        foreach (string part in customParams.Split(','))
        {
            if (
                int.TryParse(
                    part.Trim(),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int value
                )
                && value > 0
            )
            {
                steps.Add(value);
            }
        }

        return steps.Count > 0 ? [.. steps] : DefaultSteps;
    }
}
