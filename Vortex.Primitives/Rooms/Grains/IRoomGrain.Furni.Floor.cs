using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Snapshots.Furniture;
using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;
using Vortex.Primitives.Rooms.Wired.Variable;

namespace Vortex.Primitives.Rooms.Grains;

public partial interface IRoomGrain
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
        UpdateWiredMessage update,
        CancellationToken ct
    );
    public Task<RoomFloorItemSnapshot?> GetFloorItemSnapshotByIdAsync(
        RoomObjectId itemId,
        CancellationToken ct
    );
    public Task<ImmutableArray<RoomFloorItemSnapshot>> GetAllFloorItemSnapshotsAsync(
        CancellationToken ct
    );
    public Task<WiredDataSnapshot?> GetWiredDataSnapshotByFloorItemIdAsync(
        RoomObjectId itemId,
        CancellationToken ct
    );
    public Task<WiredVariablesSnapshot> GetWiredVariablesSnapshotAsync(CancellationToken ct);
    public Task<
        List<(WiredVariableId id, WiredVariableValue value)>
    > GetAllVariablesForBindingAsync(WiredVariableBinding binding, CancellationToken ct);
}
