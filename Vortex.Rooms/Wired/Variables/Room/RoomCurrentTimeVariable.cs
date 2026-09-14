using System;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Room;

/// <summary>
/// The shared half of the ten calendar readings behind <c>@current_time</c>: each subclass names
/// itself and says which field of the clock it takes.
/// </summary>
/// <remarks>
/// <c>@current_time</c> is not a timestamp. The official client offers it as a folder of ten
/// separate readings — milliseconds through year — and a builder picks the one they mean, so there
/// is no unit to agree on and no arithmetic to get wrong. The folder itself is not a thing the
/// server sends: the picker builds its tree by splitting variable names on the dot, which is the
/// same mechanism already behind <c>@position.x</c> and <c>@dimensions.y</c>. Each reading is
/// therefore an ordinary flat variable that happens to be named with dots in it.
/// <para>
/// ponytail: reads UTC. The room carries its own zone in <c>rooms.wired_timezone</c> and the
/// date/time CONDITIONS honour it — but they take it from the box's own StringParam, and a
/// room-level variable has no box. Reading the column per tick is not an option, so honouring it
/// means caching the zone in the room's live state; do that when a hotel actually sets one.
/// <see cref="WiredTimeZone.Now"/> already falls back to UTC for an unset or unknown zone, which is
/// every room today.
/// </para>
/// </remarks>
public abstract class RoomCurrentTimeVariable(RoomGrain roomGrain) : RoomVariable(roomGrain)
{
    protected override WiredVariableGroupSubBandType SubBandType =>
        WiredVariableGroupSubBandType.Base;

    protected override WiredVariableFlags Flags =>
        WiredVariableFlags.HasValue | WiredVariableFlags.AlwaysAvailable;

    protected override WiredVariableValue GetValueForRoom(RoomGrain roomGrain) =>
        WiredVariableValue.Parse(Read(WiredTimeZone.Now(null)));

    /// <summary>The one field of the clock this reading is.</summary>
    protected abstract int Read(DateTime now);
}
