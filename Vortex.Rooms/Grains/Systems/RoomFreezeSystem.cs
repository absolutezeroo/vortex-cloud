using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Messages.Outgoing.Room.Action;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Rooms.Grains.Systems.Freeze;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Freeze;

namespace Vortex.Rooms.Grains.Systems;

/// <summary>
/// Grain-side controller for the room's Freeze minigame — the thin, IO-owning wrapper around the pure
/// <see cref="RoomFreezeGame"/> (mirroring how <see cref="RoomGameSystem"/> wraps
/// <see cref="GameTeamState"/>). It turns the game's state changes into avatar-effect broadcasts, gate
/// counter updates and (later) teleports and score, and it resolves the live balance from server config
/// each round. All calls run inside the room grain's single-threaded turn, so no locking.
/// <para>
/// The Freeze game is deliberately separate from <see cref="RoomGameSystem"/> (the wired Battle-Banzai
/// style team game): players join via physical gates, carry rich per-player state (lives, ammo,
/// power-ups) and wear the Freeze effect set, none of which the generic team system models.
/// </para>
/// </summary>
public sealed class RoomFreezeSystem(RoomGrain roomGrain)
{
    private readonly RoomGrain _roomGrain = roomGrain;
    private readonly RoomFreezeGame _game = new();

    public bool IsRunning => _game.IsRunning;

    public GameTeamColor GetTeam(PlayerId playerId) => _game.GetTeam(playerId);

    /// <summary>A player walked onto a team gate — toggle their membership and reflect it (effect +
    /// gate counters). Only takes effect before the round starts.</summary>
    public async Task OnGateWalkOnAsync(PlayerId playerId, GameTeamColor team, CancellationToken ct)
    {
        FreezeGateResult result = _game.ToggleGate(playerId, team);

        if (result == FreezeGateResult.None)
        {
            return;
        }

        await BroadcastEffectAsync(
            playerId,
            result == FreezeGateResult.Joined
                ? FreezeConstants.TeamEffectBase + (int)team
                : FreezeConstants.NoEffect
        );

        await RefreshGateCountersAsync();
    }

    /// <summary>Starts the round when the game timer starts: resolves live balance, resets loadouts,
    /// applies team effects and refreshes the gate counters.</summary>
    public async Task StartGameAsync(CancellationToken ct)
    {
        _game.Settings = await FreezeConfig.ResolveAsync(
            _roomGrain._grainFactory.GetServerConfigGrain()
        );

        if (!_game.Start())
        {
            return;
        }

        foreach ((PlayerId playerId, FreezePlayerState player) in _game.Players)
        {
            await BroadcastEffectAsync(playerId, player.CurrentEffect());
        }

        await RefreshGateCountersAsync();
    }

    /// <summary>Ends the round when the game timer expires: stops the game and clears every player's
    /// effect. Returns the winning team (highest score, None on a scoreless game).</summary>
    public async Task<GameTeamColor> EndGameAsync(CancellationToken ct)
    {
        GameTeamColor winner = _game.Stop();

        foreach ((PlayerId playerId, _) in _game.Players)
        {
            await BroadcastEffectAsync(playerId, FreezeConstants.NoEffect);
        }

        await RefreshGateCountersAsync();

        return winner;
    }

    /// <summary>Drop a player who left the room from the game.</summary>
    public async Task OnPlayerLeftAsync(PlayerId playerId, CancellationToken ct)
    {
        if (_game.Remove(playerId) is not null)
        {
            await RefreshGateCountersAsync();
        }
    }

    /// <summary>Sets each team gate's displayed state to that team's living member count.</summary>
    private async Task RefreshGateCountersAsync()
    {
        foreach (IRoomItem item in _roomGrain._state.ItemsById.Values)
        {
            if (item.Logic is not FurnitureFreezeGateLogic gate)
            {
                continue;
            }

            await gate.SetStateAsync(_game.GetTeamCount(gate.TeamColor));
        }
    }

    /// <summary>Sets the avatar effect and broadcasts it so the room (and late joiners) re-syncs it.</summary>
    private Task BroadcastEffectAsync(PlayerId playerId, int effectId)
    {
        if (!_roomGrain._state.AvatarsByPlayerId.TryGetValue(playerId, out RoomObjectId objectId))
        {
            return Task.CompletedTask;
        }

        if (_roomGrain._state.AvatarsByObjectId.TryGetValue(objectId, out IRoomAvatar? avatar))
        {
            avatar.SetEffect(effectId);
        }

        return _roomGrain.SendComposerToRoomAsync(
            new AvatarEffectMessageComposer
            {
                UserId = objectId,
                EffectId = effectId,
                DelayMilliseconds = 0,
            }
        );
    }
}
