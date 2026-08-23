using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Moderation;

/// <summary>
/// Answers "what would the default sanction be", so the mod tool can show the consequence before
/// the moderator commits. <see cref="IssueId"/> and <see cref="AccountId"/> echo the request — the
/// client routes the response by whichever one is not -1.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record SanctionInfoMessageComposer : IComposer
{
    [Id(0)]
    public int IssueId { get; init; } = -1;

    [Id(1)]
    public int AccountId { get; init; } = -1;

    [Id(2)]
    public required string SanctionName { get; init; }

    [Id(3)]
    public int SanctionLengthInHours { get; init; }

    /// <summary>Whether the sanction hits the avatar only rather than the whole account.</summary>
    [Id(4)]
    public bool AvatarOnly { get; init; }

    /// <summary>Free text appended to the consequence line; blank when the sanction carries no
    /// trading lock. Written together with <see cref="MachineBanInfo"/> or not at all.</summary>
    [Id(5)]
    public string TradeLockInfo { get; init; } = string.Empty;

    [Id(6)]
    public string MachineBanInfo { get; init; } = string.Empty;
}
