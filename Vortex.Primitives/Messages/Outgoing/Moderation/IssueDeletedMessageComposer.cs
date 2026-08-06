using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Moderation;

/// <summary>
/// Drops a ticket from every moderator's open queue. The id travels as a string on the wire — the
/// client does <c>parseInt(readString())</c> — so the serializer writes it as text, not as an int.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record IssueDeletedMessageComposer : IComposer
{
    [Id(0)]
    public required int IssueId { get; init; }
}
