using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Snapshots.Furniture;
using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;
using Vortex.Primitives.Rooms.Wired.Variable;

namespace Vortex.Primitives.Rooms.Grains;

public partial interface IRoomFurni
{
    public Task<bool> PlaceFloorItemAsync(
        ActionContext ctx,
        FurnitureItemSnapshot item,
        int x,
        int y,
        Rotation rot,
        CancellationToken ct
    );
    public Task<bool> MoveFloorItemByIdAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        int x,
        int y,
        Rotation rot,
        CancellationToken ct
    );
    public Task<bool> ApplyWiredUpdateAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        WiredUpdateRequest update,
        CancellationToken ct
    );
    public Task<RoomFloorItemSnapshot?> GetFloorItemSnapshotByIdAsync(
        RoomObjectId itemId,
        CancellationToken ct
    );
    public Task<ImmutableArray<RoomFloorItemSnapshot>> GetAllFloorItemSnapshotsAsync(
        CancellationToken ct
    );

    /// <param name="playerId">Who asked. A reference-variable box is answered with the variables
    /// that player's own rooms share, so the snapshot depends on the reader, not only on the box.</param>
    public Task<WiredDataSnapshot?> GetWiredDataSnapshotByFloorItemIdAsync(
        RoomObjectId itemId,
        PlayerId playerId,
        CancellationToken ct
    );
    public Task<WiredVariablesSnapshot> GetWiredVariablesSnapshotAsync(CancellationToken ct);
    public Task<
        List<(WiredVariableId id, WiredVariableValue value)>
    > GetAllVariablesForBindingAsync(WiredVariableBinding binding, CancellationToken ct);
}
