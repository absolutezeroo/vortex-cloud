using Orleans;

namespace Vortex.Primitives.Help;

/// <summary>
/// A live guide session: who asked, who took it, and what it is about.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record GuideSessionSnapshot
{
    [Id(0)]
    public required int RequesterId { get; init; }

    [Id(1)]
    public required int GuideId { get; init; }

    /// <summary>0 and 2 are tour requests, 1 is a help request — the client's own three entry
    /// points into <c>createHelpRequest</c>.</summary>
    [Id(2)]
    public required int HelpRequestType { get; init; }

    [Id(3)]
    public required string Description { get; init; }
}
