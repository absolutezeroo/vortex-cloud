using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Room;

public abstract class RoomVariable(RoomGrain roomGrain) : WiredInternalVariable(roomGrain)
{
    protected override WiredVariableTargetType TargetType => WiredVariableTargetType.Global;

    public override bool TryGetValue(in WiredVariableKey key, out WiredVariableValue value)
    {
        value = WiredVariableValue.Default;

        if (!CanBind(key))
        {
            return false;
        }

        value = GetValueForRoom(_roomGrain);

        return true;
    }

    protected abstract WiredVariableValue GetValueForRoom(RoomGrain roomGrain);
}
