using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Messages.Outgoing.Room.Action;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Rooms.Grains.Systems;

/// <summary>Owns the wired game team membership and team scores for a room. All state lives in the pure
/// <see cref="GameTeamState"/> (the single source of truth, unit-tested in isolation); this class is the
/// thin grain wrapper that turns membership changes into client team-aura effect broadcasts.
///
/// The state is ephemeral — constructed fresh each time the room grain activates, so a game naturally
/// dies with the room, matching Habbo. Every mutation runs inside the room grain's single-threaded turn,
/// so no locking is needed. The team aura is mirrored to the client with effect id <c>32 + team</c>
/// (Red=33, Green=34, Blue=35, Yellow=36) via <see cref="AvatarEffectMessageComposer"/>.</summary>
public sealed class RoomGameSystem(RoomGrain roomGrain)
{
    private const int TeamEffectBase = 32; // Red(1)->33, Green(2)->34, Blue(3)->35, Yellow(4)->36
    private const int NoEffect = 0;

    private readonly RoomGrain _roomGrain = roomGrain;
    private readonly GameTeamState _state = new();

    public GameTeamColor GetTeam(PlayerId playerId) => _state.GetTeam(playerId);

    public int GetTeamScore(GameTeamColor team) => _state.GetTeamScore(team);

    public int GetTeamMemberCount(GameTeamColor team) => _state.GetTeamMemberCount(team);

    public IReadOnlyList<PlayerId> GetPlayersInTeam(GameTeamColor team) =>
        _state.GetPlayersInTeam(team);

    public GameTeamColor GetSmallestTeam() => _state.GetSmallestTeam();

    public Task JoinTeamAsync(PlayerId playerId, GameTeamColor team, CancellationToken ct) =>
        _state.JoinTeam(playerId, team)
            ? BroadcastEffectAsync(playerId, TeamEffectBase + (int)team)
            : Task.CompletedTask;

    public Task LeaveTeamAsync(PlayerId playerId, CancellationToken ct) =>
        _state.LeaveTeam(playerId) ? BroadcastEffectAsync(playerId, NoEffect) : Task.CompletedTask;

    public bool TryGiveScoreToPlayerTeam(
        RoomObjectId box,
        PlayerId playerId,
        int amount,
        int cap
    ) => _state.TryGiveScoreToPlayerTeam(box, playerId, amount, cap);

    public bool TryGiveScoreToTeam(RoomObjectId box, GameTeamColor team, int amount, int cap) =>
        _state.TryGiveScoreToTeam(box, team, amount, cap);

    /// <summary>Clears membership when a player leaves the room, so team state never outlives a player's
    /// presence. No effect broadcast — the avatar is already gone.</summary>
    public void OnPlayerLeft(PlayerId playerId) => _state.OnPlayerLeft(playerId);

    /// <summary>Wipes teams, scores and caps (called when a game starts/restarts), clearing every
    /// current member's aura first so no stale effect lingers.</summary>
    public async Task ResetGameAsync(CancellationToken ct)
    {
        foreach (PlayerId playerId in _state.Reset())
        {
            await BroadcastEffectAsync(playerId, NoEffect);
        }
    }

    private Task BroadcastEffectAsync(PlayerId playerId, int effectId)
    {
        if (!_roomGrain._state.AvatarsByPlayerId.TryGetValue(playerId, out RoomObjectId objectId))
        {
            return Task.CompletedTask;
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
