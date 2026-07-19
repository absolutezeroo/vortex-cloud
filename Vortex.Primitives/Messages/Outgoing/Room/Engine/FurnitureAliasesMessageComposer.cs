using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Room.Engine;

[GenerateSerializer, Immutable]
public sealed record FurnitureAliasesMessageComposer : IComposer
{
    [Id(0)]
    public required List<(string, string)> Aliases { get; init; }
}
