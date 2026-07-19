using System.Threading.Tasks;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Furniture.Wall;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Furniture;

public sealed class FurnitureAltitudeVariable(RoomGrain roomGrain)
    : FurnitureVariable<IRoomItem>(roomGrain)
{
    protected override string VariableName => "@altitude";
    protected override WiredVariableGroupSubBandType SubBandType =>
        WiredVariableGroupSubBandType.Position;
    protected override ushort Order => 10;
    protected override WiredVariableFlags Flags =>
        WiredVariableFlags.HasValue
        | WiredVariableFlags.CanWriteValue
        | WiredVariableFlags.AlwaysAvailable;

    protected override bool TryGetValueForItem(IRoomItem item, out WiredVariableValue value)
    {
        value = item.Z.ToInt();

        return true;
    }

    public override async Task<bool> SetValueAsync(
        IWiredExecutionContext ctx,
        WiredVariableKey key,
        WiredVariableValue value
    )
    {
        WiredVariableSnapshot snapshot = GetVarSnapshot();

        if (
            !snapshot.Flags.Has(WiredVariableFlags.CanWriteValue)
            || !CanBind(key)
            || !TryGetItemForKey(key, out IRoomItem? item)
        )
        {
            return false;
        }

        switch (item)
        {
            case IRoomFloorItem floorItem:
                {
                    if (
                        await _roomGrain.FurniModule.ValidateFloorItemPlacementAsync(
                            ctx.AsActionContext(),
                            floorItem.ObjectId,
                            floorItem.X,
                            floorItem.Y,
                            floorItem.Rotation
                        )
                    )
                    {
                        await ctx.ProcessFloorItemMovementAsync(
                            floorItem,
                            _roomGrain.MapModule.ToIdx(floorItem.X, floorItem.Y),
                            Altitude.FromInt(value),
                            floorItem.Rotation
                        );

                        return true;
                    }
                }
                break;
            case IRoomWallItem wallItem:
                {
                    if (
                        await _roomGrain.FurniModule.ValidateWallItemPlacementAsync(
                            ctx.AsActionContext(),
                            wallItem.ObjectId,
                            wallItem.X,
                            wallItem.Y,
                            Altitude.FromInt(value),
                            wallItem.WallOffset,
                            wallItem.Rotation
                        )
                    )
                    {
                        await ctx.ProcessWallItemMovementAsync(
                            wallItem,
                            wallItem.X,
                            wallItem.Y,
                            Altitude.FromInt(value),
                            wallItem.Rotation,
                            wallItem.WallOffset
                        );

                        return true;
                    }
                }
                break;
        }

        return false;
    }
}
