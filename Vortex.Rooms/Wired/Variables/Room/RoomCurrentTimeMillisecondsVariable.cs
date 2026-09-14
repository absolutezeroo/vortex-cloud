using System;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Room;

public sealed class RoomCurrentTimeMillisecondsVariable(RoomGrain roomGrain)
    : RoomCurrentTimeVariable(roomGrain)
{
    protected override string VariableName => "@current_time.milliseconds_of_seconds";
    protected override ushort Order => 70;

    protected override int Read(DateTime now) => now.Millisecond;
}
