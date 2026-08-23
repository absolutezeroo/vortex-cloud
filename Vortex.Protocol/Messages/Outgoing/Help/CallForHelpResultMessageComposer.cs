using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Help;

[GenerateSerializer, Immutable]
public sealed record CallForHelpResultMessageComposer : IComposer
{
    [Id(0)]
    public required int ResultType { get; init; }

    [Id(1)]
    public required string MessageText { get; init; }
}
