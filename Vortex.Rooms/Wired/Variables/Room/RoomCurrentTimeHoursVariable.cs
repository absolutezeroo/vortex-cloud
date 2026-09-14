using System;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Room;

public sealed class RoomCurrentTimeHoursVariable(RoomGrain roomGrain)
    : RoomCurrentTimeVariable(roomGrain)
{
    protected override string VariableName => "@current_time.hour_of_day";
    protected override ushort Order => 73;

    protected override int Read(DateTime now) => now.Hour;
}
