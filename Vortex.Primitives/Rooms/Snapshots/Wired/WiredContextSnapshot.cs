using System.Collections.Generic;
using Orleans;

namespace Vortex.Primitives.Rooms.Snapshots.Wired;

[GenerateSerializer, Immutable]
public sealed record WiredContextSnapshot
{
    [Id(0)]
    public required Dictionary<string, int> Variables { get; init; }

    [Id(1)]
    public required WiredSelectionSetSnapshot Selected { get; init; }
}
