using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Furniture;

public sealed class FurnitureIsInvisibleVariable(RoomGrain roomGrain)
    : FurnitureVariable<IRoomItem>(roomGrain)
{
    protected override string VariableName => "@is_invisible";
    protected override WiredVariableGroupSubBandType SubBandType =>
        WiredVariableGroupSubBandType.Meta;
    protected override ushort Order => 80;
    protected override WiredVariableFlags Flags => WiredVariableFlags.None;

    protected override bool TryGetValueForItem(IRoomItem item, out WiredVariableValue value)
    {
        value = WiredVariableValue.Default;

        return item.IsInvisible;
    }
}
