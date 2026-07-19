using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Rooms.Wired.Variable;

namespace Vortex.Primitives.Rooms.Snapshots.Wired.Variables;

[GenerateSerializer, Immutable]
public sealed record WiredVariablesSnapshot
{
    [Id(0)]
    public required WiredVariableHash AllVariablesHash { get; init; }

    [Id(1)]
    public required List<WiredVariableSnapshot> Variables { get; init; }
}
