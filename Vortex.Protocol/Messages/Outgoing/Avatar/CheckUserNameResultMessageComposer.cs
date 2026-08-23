using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Avatar;

/// <summary>
/// Answers <see cref="Incoming.Avatar.CheckUserNameMessage"/>. The client only reads the result
/// code and matches <see cref="Name"/> against what is currently typed — an answer for a name the
/// user has already typed past is dropped, so the name must be echoed back.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record CheckUserNameResultMessageComposer : IComposer
{
    [Id(0)]
    public required int ResultCode { get; init; }

    [Id(1)]
    public required string Name { get; init; }

    [Id(2)]
    public required ImmutableArray<string> NameSuggestions { get; init; }
}
