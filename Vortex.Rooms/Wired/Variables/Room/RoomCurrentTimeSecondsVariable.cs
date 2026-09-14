using System;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Room;

public sealed class RoomCurrentTimeSecondsVariable(RoomGrain roomGrain)
    : RoomCurrentTimeVariable(roomGrain)
{
    protected override string VariableName => "@current_time.seconds_of_minute";
    protected override ushort Order => 71;

    protected override int Read(DateTime now) => now.Second;
}
