using System.Collections.Generic;
using Orleans;

namespace Vortex.Primitives.Rooms.Snapshots.Wired.Variables;

/// <summary>
/// One variable a room shares with the rest of its owner's rooms, tagged with where it comes from.
/// </summary>
/// <remarks>
/// The room is carried per variable rather than per room because that is the shape the client reads:
/// <c>SharedVariableList</c> is a flat list of (room id, room name, variable) and groups it by room
/// itself, in <c>ReferenceVariable.initRooms()</c>. This is no longer a context section of its own —
/// one shared variable is never sent alone — but the entry inside <see cref="WiredVariableSharedListSnapshot"/>.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record WiredVariableSharedSnapshot
{
    [Id(0)]
    public required WiredVariableSnapshot Variable { get; init; }

    [Id(1)]
    public required int RoomId { get; init; }

    [Id(2)]
    public required string RoomName { get; init; }
}

/// <summary>
/// Everything a reference-variable box may point at: every variable shared by the rooms of the
/// player who opened it.
/// </summary>
/// <remarks>
/// Sent as its own wired-context section. Its absence is not the same as an empty list — the client
/// disables the whole dialog when the section is missing (<c>ReferenceVariable.setEditable()</c> is
/// called with <c>context != null</c>), so a player with no shared variables must still get this
/// section, holding nothing.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record WiredVariableSharedListSnapshot : WiredVariableContextSnapshot
{
    [Id(1)]
    public required List<WiredVariableSharedSnapshot> Elements { get; init; }
}
