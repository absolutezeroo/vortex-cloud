using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Games;
using Vortex.Rooms.Games.Arena;

namespace Vortex.Rooms.Games.Abstractions;

/// <summary>
/// The base a game module derives from: no-op virtuals across the whole <see cref="IRoomGame"/>
/// surface so a game overrides only the hooks it uses, and the context stored once.
/// <para>
/// Kept as an abstract class rather than default interface methods to match the codebase's
/// established pattern, and because a module wants the context field anyway.
/// </para>
/// </summary>
public abstract class RoomGameModule(IRoomGameContext context) : IRoomGame
{
    protected readonly IRoomGameContext _context = context;

    public abstract GameProfile Profile { get; }

    /// <summary>Whether this game's rules are live. The one question every gameplay path asks, and
    /// the reason no module carries an <c>IsRunning</c> of its own.</summary>
    protected bool IsLive => Runtime.GameStateMachine.IsLive(_context.Phase);

    /// <summary>Whether a match exists at all — what a team gate asks to know it should be inert.</summary>
    protected bool HasMatch => Runtime.GameStateMachine.HasMatch(_context.Phase);

    /// <summary>An arena nothing is missing from. A game with no furniture requirements at all keeps
    /// this; anything with an arena overrides it and says what it needs.</summary>
    public virtual ArenaValidation ValidateArena() => ArenaValidation.Valid;

    public virtual Task OnPreparingAsync(GameMatch match, CancellationToken ct) =>
        Task.CompletedTask;

    public virtual Task OnStartedAsync(GameMatch match, CancellationToken ct) => Task.CompletedTask;

    public virtual Task OnRoundEndingAsync(GameMatch match, CancellationToken ct) =>
        Task.CompletedTask;

    public virtual Task OnResettingAsync(GameMatch match, CancellationToken ct) =>
        Task.CompletedTask;

    public virtual Task TickAsync(long nowMs, CancellationToken ct) => Task.CompletedTask;

    public virtual Task OnSignalAsync(GameSignal signal, CancellationToken ct) => Task.CompletedTask;

    public virtual Task OnParticipantLeftAsync(PlayerId playerId, CancellationToken ct) =>
        Task.CompletedTask;

    public virtual Task OnParticipantEnteredAsync(PlayerId playerId, CancellationToken ct) =>
        Task.CompletedTask;
}
