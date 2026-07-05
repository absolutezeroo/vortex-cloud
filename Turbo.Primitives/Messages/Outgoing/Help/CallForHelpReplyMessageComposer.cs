using Orleans;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Help;

[GenerateSerializer, Immutable]
public sealed record CallForHelpReplyMessageComposer : IComposer
{
    [Id(0)]
    public required string Message { get; init; }
}
