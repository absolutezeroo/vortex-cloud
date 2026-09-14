using System;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Room;

public sealed class RoomCurrentTimeDayOfYearVariable(RoomGrain roomGrain)
    : RoomCurrentTimeVariable(roomGrain)
{
    protected override string VariableName => "@current_time.day_of_year";
    protected override ushort Order => 76;

    protected override int Read(DateTime now) => now.DayOfYear;
}
