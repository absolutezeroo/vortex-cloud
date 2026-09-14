using System;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Room;

public sealed class RoomCurrentTimeMonthVariable(RoomGrain roomGrain)
    : RoomCurrentTimeVariable(roomGrain)
{
    protected override string VariableName => "@current_time.month_of_year";
    protected override ushort Order => 78;

    protected override int Read(DateTime now) => now.Month;
}
