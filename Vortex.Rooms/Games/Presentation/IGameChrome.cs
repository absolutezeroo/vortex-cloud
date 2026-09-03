using System.Threading.Tasks;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums.Games;

namespace Vortex.Rooms.Games.Presentation;

/// <summary>
/// Everything a game shows a client, in one place. A game rule never builds a composer; it says
/// "this player is frozen" or "this player has three lives left" and the chrome decides which
/// message that is. That split is what lets the rules be tested with no room, no session and no
/// protocol at all.
/// <para>
/// None of these dedupe. A re-broadcast is how a match re-asserts an aura a player already wears
/// after a reconnect, which is exactly what the room's own effect setter must not do.
/// </para>
/// </summary>
public interface IGameChrome
{
    /// <summary>Persists the effect on the avatar (so a late joiner's snapshot re-syncs it) and
    /// broadcasts it to the room.</summary>
    Task BroadcastEffectAsync(PlayerId playerId, int effectId);

    /// <summary>Applies the team aura from the given effect set — or clears the effect when the
    /// player is on no team.</summary>
    Task BroadcastTeamAuraAsync(PlayerId playerId, GameAuraSet aura, GameTeamColor team);

    Task ClearEffectAsync(PlayerId playerId);

    /// <summary>Flips the client's "game mode" (the generic YouArePlayingGame message — no room game
    /// has a bespoke HUD protocol in this client).</summary>
    Task SetPlayingModeAsync(PlayerId playerId, bool isPlaying);

    /// <summary>The leave-path variant of <see cref="SetPlayingModeAsync"/>, and the ONLY sanctioned
    /// way to clear game mode from a player-left hook. It is deliberately not awaited: player-left
    /// runs while the leaver's own presence grain is mid-call into this room — including from
    /// OnDeactivateAsync, where the activation no longer dispatches incoming requests — so awaiting
    /// a call back into it would hang the room's turn (every player in it) until the 30 s Orleans
    /// call timeout.</summary>
    void SetPlayingModeAndForget(PlayerId playerId, bool isPlaying);

    /// <summary>Shows <paramref name="value"/> as a number bubble over the player's avatar (0 clears
    /// it) — Freeze uses it for remaining lives.</summary>
    Task BroadcastPlayerValueAsync(PlayerId playerId, int value);

    /// <summary>Roots the player where they stand: locks new walks and cancels the in-flight one.
    /// The wired freeze-user box and a Freeze hit both come through here so "frozen" means one
    /// thing. The lock lives on the avatar, so it dies with the room presence and cannot leak.</summary>
    void LockMovement(PlayerId playerId);

    /// <summary>Releases a movement lock (wired unfreeze-user, a thaw, a match ending).</summary>
    void UnlockMovement(PlayerId playerId);

    /// <summary>Resets every game-timer furni's countdown and <c>gameActive</c> flag. Needed after a
    /// match that ended early; on a normal expiry the furni resets itself.</summary>
    void ResetGameTimers();
}
