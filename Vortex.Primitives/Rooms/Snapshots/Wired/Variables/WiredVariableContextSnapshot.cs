using Orleans;
using Vortex.Primitives.Rooms.Enums.Wired;

namespace Vortex.Primitives.Rooms.Snapshots.Wired.Variables;

[GenerateSerializer, Immutable]
public abstract record WiredVariableContextSnapshot
{
    [Id(0)]
    public required WiredContextType ContextType { get; init; }
}
