using System;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Room;

public sealed class RoomCurrentTimeDayOfMonthVariable(RoomGrain roomGrain)
    : RoomCurrentTimeVariable(roomGrain)
{
    protected override string VariableName => "@current_time.day_of_month";
    protected override ushort Order => 75;

    protected override int Read(DateTime now) => now.Day;
}
