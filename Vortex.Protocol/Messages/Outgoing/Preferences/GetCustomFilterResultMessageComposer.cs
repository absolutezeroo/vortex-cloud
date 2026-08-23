using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Preferences;

/// <summary>
/// The player's whole personal word filter, answering
/// <see cref="Incoming.Preferences.GetCustomFilterMessage"/>.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record GetCustomFilterResultMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<string> Words { get; init; }
}
