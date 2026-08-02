using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Action;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Rooms.Grains;

/// <summary>Mystery boxes and mystery trophies placed in the room.</summary>
[Alias("Vortex.Primitives.Rooms.Grains.IRoomMysteryBox")]
public interface IRoomMysteryBox : IGrainWithIntegerKey
{
    /// <summary>
    /// Handles a click on a mystery box. The box's owner and any holder of a key in the box's colour
    /// each register their half; whoever arrives first gets the wait dialog, and the box opens the
    /// moment both halves are present -- consuming the key and the box, and awarding one prize to
    /// each participant.
    /// </summary>
    public Task UseMysteryBoxAsync(
        ActionContext ctx,
        RoomObjectId boxObjectId,
        CancellationToken ct
    );

    /// <summary>
    /// Withdraws the caller from the pending open identified by <paramref name="boxOwnerId"/> (the
    /// only identifier the client's cancel message carries). When the owner withdraws the whole
    /// session ends; when a key holder does, the box stays reserved for the owner and frees up for
    /// another key holder.
    /// </summary>
    public Task CancelMysteryBoxWaitAsync(
        ActionContext ctx,
        PlayerId boxOwnerId,
        CancellationToken ct
    );

    /// <summary>
    /// Opens an inscribed mystery trophy: consumes the trophy furniture and grants its owner a
    /// random trophy from the trophy pool, carrying <paramref name="inscription"/>.
    /// </summary>
    public Task OpenMysteryTrophyAsync(
        ActionContext ctx,
        RoomObjectId objectId,
        string inscription,
        CancellationToken ct
    );
}
