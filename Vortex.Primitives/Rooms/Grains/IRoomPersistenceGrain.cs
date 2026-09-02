using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Pets.Snapshots;
using Vortex.Primitives.Rooms.Snapshots.Furniture;

namespace Vortex.Primitives.Rooms.Grains;

public interface IRoomPersistenceGrain : IGrainWithIntegerKey
{
    public Task EnqueueDirtyItemAsync(
        RoomId roomId,
        RoomItemSnapshot snapshot,
        CancellationToken ct,
        bool remove = false
    );
    public Task EnqueueDirtyItemsAsync(
        RoomId roomId,
        List<RoomItemSnapshot> snapshots,
        CancellationToken ct
    );

    /// <summary>
    /// The pets whose stats have moved, written on the same clock as the furniture.
    /// </summary>
    /// <remarks>
    /// Pets used to write themselves: the room opened a <c>DbContext</c> and saved inside its own
    /// tick, every sixty seconds, so a room full of pets paid for a database round trip in the turn
    /// that was supposed to be moving avatars (PET-TICK-044). The furniture answered this years
    /// earlier with this grain; a comment in the pet code even claimed pets used the same pattern,
    /// which was the belief that kept it unfixed.
    /// </remarks>
    public Task EnqueueDirtyPetsAsync(
        RoomId roomId,
        List<PetSnapshot> snapshots,
        CancellationToken ct
    );
}
