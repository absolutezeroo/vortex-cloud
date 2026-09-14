using System;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Room;

/// <summary>
/// Monday is 1 and Sunday is 7.
/// </summary>
/// <remarks>
/// .NET numbers Sunday 0, which would put the end of the week before its start for anyone building
/// a weekend check — the most obvious thing to build with this reading.
/// </remarks>
public sealed class RoomCurrentTimeDayOfWeekVariable(RoomGrain roomGrain)
    : RoomCurrentTimeVariable(roomGrain)
{
    protected override string VariableName => "@current_time.day_of_week";
    protected override ushort Order => 74;

    protected override int Read(DateTime now) =>
        now.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)now.DayOfWeek;
}
