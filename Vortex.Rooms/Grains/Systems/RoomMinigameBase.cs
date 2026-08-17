using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Players;

namespace Vortex.Rooms.Grains.Systems;

/// <summary>
/// The base every room game derives from: no-op virtuals for the whole <see cref="IRoomMinigame"/>
/// surface, so a game only overrides the hooks it actually uses (the interface stays the contract the
/// coordinator drives; this base is the convenience). Kept as an abstract class rather than default
/// interface methods to match the codebase's established pattern.
/// </summary>
public abstract class RoomMinigameBase(RoomGrain roomGrain) : IRoomMinigame
{
    protected readonly RoomGrain _roomGrain = roomGrain;

    public abstract string Name { get; }

    public virtual Task StartAsync(CancellationToken ct) => Task.CompletedTask;

    public virtual Task EndAsync(CancellationToken ct) => Task.CompletedTask;

    public virtual Task TickAsync(long nowMs, CancellationToken ct) => Task.CompletedTask;

    public virtual Task OnPlayerLeftAsync(PlayerId playerId, CancellationToken ct) =>
        Task.CompletedTask;

    public virtual Task OnPlayerEnteredAsync(PlayerId playerId, CancellationToken ct) =>
        Task.CompletedTask;
}
