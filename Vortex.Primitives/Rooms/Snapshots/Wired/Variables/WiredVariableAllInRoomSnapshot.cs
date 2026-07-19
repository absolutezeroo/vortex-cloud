using Orleans;
using Vortex.Primitives.Rooms.Wired.Variable;

namespace Vortex.Primitives.Rooms.Snapshots.Wired.Variables;

[GenerateSerializer, Immutable]
public record WiredVariableAllInRoomSnapshot : WiredVariableContextSnapshot
{
    [Id(1)]
    public required WiredVariableHash AllVariablesHash { get; init; }
}
