using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Moderator;

/// <summary>Asks what the default sanction would be, before the moderator commits to it. Exactly
/// one of <see cref="IssueId"/> / <see cref="AccountId"/> is set; the other arrives as -1.</summary>
public record ModToolSanctionMessage : IMessageEvent
{
    public int IssueId { get; init; } = -1;
    public int AccountId { get; init; } = -1;
    public int CategoryId { get; init; }
}
