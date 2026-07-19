using System.Collections.Generic;
using Orleans;

namespace Vortex.Primitives.Groups.Snapshots;

/// <summary>A page of forums for the client's ForumsList (cross-group browse).</summary>
[GenerateSerializer, Immutable]
public sealed record ForumsListPageSnapshot
{
    [Id(0)]
    public required int ListCode { get; init; }

    [Id(1)]
    public required int TotalAmount { get; init; }

    [Id(2)]
    public required int StartIndex { get; init; }

    [Id(3)]
    public required List<ForumSnapshot> Forums { get; init; }
}

/// <summary>A page of threads for one group's forum (client ForumThreads).</summary>
[GenerateSerializer, Immutable]
public sealed record ForumThreadsPageSnapshot
{
    [Id(0)]
    public required int GroupId { get; init; }

    [Id(1)]
    public required int StartIndex { get; init; }

    [Id(2)]
    public required List<ForumThreadSnapshot> Threads { get; init; }
}

/// <summary>A page of messages within a thread (client ThreadMessages).</summary>
[GenerateSerializer, Immutable]
public sealed record ThreadMessagesPageSnapshot
{
    [Id(0)]
    public required int GroupId { get; init; }

    [Id(1)]
    public required int ThreadId { get; init; }

    [Id(2)]
    public required int StartIndex { get; init; }

    [Id(3)]
    public required List<ForumPostSnapshot> Messages { get; init; }
}

/// <summary>
/// Outcome of a post action: either a brand-new thread (client PostThread) or a reply added to an
/// existing thread (client PostMessage). Exactly one of <see cref="Thread"/>/<see cref="Post"/> set.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record ForumPostResultSnapshot
{
    [Id(0)]
    public required int GroupId { get; init; }

    [Id(1)]
    public required bool IsNewThread { get; init; }

    [Id(2)]
    public ForumThreadSnapshot? Thread { get; init; }

    [Id(3)]
    public int ThreadId { get; init; }

    [Id(4)]
    public ForumPostSnapshot? Post { get; init; }
}
