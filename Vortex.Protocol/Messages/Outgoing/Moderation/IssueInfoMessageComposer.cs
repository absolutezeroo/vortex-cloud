using Orleans;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Moderation;

/// <summary>
/// A single ticket pushed to the mod tool outside the login queue — sent when a ticket is picked,
/// or when its state changes while a moderator has it open. The payload is one issue block, the
/// same shape ModeratorInitMessageComposer repeats per queue entry.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record IssueInfoMessageComposer : IComposer
{
    [Id(0)]
    public required CfhIssueQueueEntrySnapshot Issue { get; init; }
}
