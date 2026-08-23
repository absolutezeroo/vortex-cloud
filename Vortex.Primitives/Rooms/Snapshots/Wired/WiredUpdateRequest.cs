using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Rooms.Enums.Wired;

namespace Vortex.Primitives.Rooms.Snapshots.Wired;

/// <summary>
/// A player's reconfiguration of one wired box, as the room engine consumes it.
///
/// The six <c>Update*Message</c> types the client sends are empty subclasses of a common base: they
/// exist so the message registry can tell an action from a trigger, and nothing downstream ever
/// inspects which one arrived — the engine reads these fields and no more. So the grain takes the
/// fields rather than the packet, and the wire type stops being part of a contract in the hub every
/// project builds against.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record WiredUpdateRequest
{
    [Id(0)]
    public required int Id { get; init; }

    [Id(1)]
    public required List<int> IntParams { get; init; }

    [Id(2)]
    public required string StringParam { get; init; }

    [Id(3)]
    public required List<int> StuffIds { get; init; }

    [Id(4)]
    public required List<int> StuffIds2 { get; init; }

    [Id(5)]
    public required List<object> DefinitionSpecifics { get; init; }

    [Id(6)]
    public required List<WiredFurniSourceType[]> FurniSources { get; init; }

    [Id(7)]
    public required List<WiredPlayerSourceType[]> PlayerSources { get; init; }

    [Id(8)]
    public required List<string> VariableIds { get; init; }

    [Id(9)]
    public required List<object> TypeSpecifics { get; init; }
}
