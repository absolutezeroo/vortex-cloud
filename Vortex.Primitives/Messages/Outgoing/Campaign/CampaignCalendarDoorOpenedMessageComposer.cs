using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Campaign;

/// <summary>
/// The result of opening an advent-calendar door (header 2164).
///
/// Shape from WIN63's parser (unknowns/_SafePkg_3317/_SafeCls_3336.as): a boolean then three
/// strings. All three are read unconditionally, so a refused door still writes them — empty.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record CampaignCalendarDoorOpenedMessageComposer : IComposer
{
    [Id(0)]
    public required bool DoorOpened { get; init; }

    [Id(1)]
    public required string ProductName { get; init; }

    /// <summary>Optional artwork shown instead of the furniture icon.</summary>
    [Id(2)]
    public required string CustomImage { get; init; }

    [Id(3)]
    public required string FurnitureClassName { get; init; }
}
