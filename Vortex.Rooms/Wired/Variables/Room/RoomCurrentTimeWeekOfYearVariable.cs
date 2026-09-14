using System;
using System.Globalization;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Room;

/// <summary>
/// The ISO-8601 week number.
/// </summary>
/// <remarks>
/// Monday opens the week and week 1 is the one holding the first Thursday — what every calendar a
/// player looks at agrees on, and what <see cref="ISOWeek"/> implements. .NET's other week
/// calculations disagree at the turn of the year.
/// </remarks>
public sealed class RoomCurrentTimeWeekOfYearVariable(RoomGrain roomGrain)
    : RoomCurrentTimeVariable(roomGrain)
{
    protected override string VariableName => "@current_time.week_of_year";
    protected override ushort Order => 77;

    protected override int Read(DateTime now) => ISOWeek.GetWeekOfYear(now);
}
