using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Moderation;

/// <summary>
/// Rejection for a pick request: the listed tickets were already taken, and by whom. Only the id
/// and picker fields of each issue block are populated — the client fills the rest with zeros and
/// uses the entry purely to name the moderator who got there first.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record IssuePickFailedMessageComposer : IComposer
{
    [Id(0)]
    public ImmutableArray<IssuePickConflict> Conflicts { get; init; } =
        ImmutableArray<IssuePickConflict>.Empty;

    /// <summary>Whether the client offers the moderator a retry button.</summary>
    [Id(1)]
    public bool RetryEnabled { get; init; }

    [Id(2)]
    public int RetryCount { get; init; }
}

[GenerateSerializer, Immutable]
public sealed record IssuePickConflict
{
    [Id(0)]
    public required int IssueId { get; init; }

    [Id(1)]
    public required int PickerUserId { get; init; }

    [Id(2)]
    public required string PickerUserName { get; init; }
}
