using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>
/// Warns a Builders Club member before a placement that will cost them a furniture slot
/// (header 2458).
///
/// Shape from WIN63's parser (unknowns/_SafePkg_2184/_SafeCls_2463.as): three ints and a string,
/// then a branch on the type code - 0 (floor) is followed by x, y and direction; anything else by a
/// single wall-location string. The two tails are different lengths, so the type code has to agree
/// with what is written after it.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record BuildersClubPlacementWarningMessageComposer : IComposer
{
    /// <summary>0 places on the floor, 1 on a wall.</summary>
    [Id(0)]
    public required int TypeCode { get; init; }

    [Id(1)]
    public required int PageId { get; init; }

    [Id(2)]
    public required int OfferId { get; init; }

    [Id(3)]
    public required string ExtraParam { get; init; }

    /// <summary>Floor placements only.</summary>
    [Id(4)]
    public int X { get; init; }

    /// <summary>Floor placements only.</summary>
    [Id(5)]
    public int Y { get; init; }

    /// <summary>Floor placements only.</summary>
    [Id(6)]
    public int Direction { get; init; }

    /// <summary>Wall placements only, in the client's wall-location form.</summary>
    [Id(7)]
    public string WallLocation { get; init; } = string.Empty;
}
