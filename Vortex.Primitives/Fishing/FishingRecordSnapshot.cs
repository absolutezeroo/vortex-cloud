using Orleans;

namespace Vortex.Primitives.Fishing;

/// <summary>
/// One line of the Fishopedia: a species this player has caught, and their best of it.
/// </summary>
/// <remarks>
/// Vortex-specific: no AS3 or Habbo equivalent. The contract is the client's own
/// <c>VortexFishingRecordsMessageParser</c>.
///
/// <para>Only caught species have a record, so the book marks an entry undiscovered by the absence
/// of a row rather than by a flag.</para>
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record FishingRecordSnapshot
{
    [Id(0)]
    public required int SpeciesId { get; init; }

    [Id(1)]
    public required int BestWeight { get; init; }

    [Id(2)]
    public required int CaughtCount { get; init; }

    /// <summary>Unix seconds. When the best weight was set, not when the species was first caught.</summary>
    [Id(3)]
    public required int BestAt { get; init; }
}
