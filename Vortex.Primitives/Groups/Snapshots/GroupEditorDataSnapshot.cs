using System.Collections.Generic;
using Orleans;

namespace Vortex.Primitives.Groups.Snapshots;

/// <summary>
/// Badge-editor palette data for the client's <c>GuildEditorDataMessageEvent</c>: the available
/// base parts, layer parts, and the badge/primary/secondary color swatches.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record GroupEditorDataSnapshot
{
    [Id(0)]
    public required List<GroupBadgePartOptionSnapshot> BaseParts { get; init; }

    [Id(1)]
    public required List<GroupBadgePartOptionSnapshot> LayerParts { get; init; }

    [Id(2)]
    public required List<GroupColorOptionSnapshot> BadgeColors { get; init; }

    [Id(3)]
    public required List<GroupColorOptionSnapshot> PrimaryColors { get; init; }

    [Id(4)]
    public required List<GroupColorOptionSnapshot> SecondaryColors { get; init; }
}

/// <summary>A selectable badge part: an id and its (mask) asset file names.</summary>
[GenerateSerializer, Immutable]
public sealed record GroupBadgePartOptionSnapshot
{
    [Id(0)]
    public required int Id { get; init; }

    [Id(1)]
    public required string FileName { get; init; }

    [Id(2)]
    public required string MaskFileName { get; init; }
}

/// <summary>A selectable color swatch: an id and its hex string (client parses base-16).</summary>
[GenerateSerializer, Immutable]
public sealed record GroupColorOptionSnapshot
{
    [Id(0)]
    public required int Id { get; init; }

    [Id(1)]
    public required string ColorHex { get; init; }
}
