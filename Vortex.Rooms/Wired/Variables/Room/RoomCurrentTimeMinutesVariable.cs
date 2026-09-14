using System;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Room;

public sealed class RoomCurrentTimeMinutesVariable(RoomGrain roomGrain)
    : RoomCurrentTimeVariable(roomGrain)
{
    protected override string VariableName => "@current_time.minute_of_hour";
    protected override ushort Order => 72;

    protected override int Read(DateTime now) => now.Minute;
}
