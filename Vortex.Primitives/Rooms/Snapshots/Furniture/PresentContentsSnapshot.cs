using Orleans;

namespace Vortex.Primitives.Rooms.Snapshots.Furniture;

/// <summary>
/// A present's private record: what it holds, and how it is wrapped.
/// </summary>
/// <remarks>
/// The offer is named rather than its products, because that is what was paid for: a bundle gift
/// has to grant every product in it, and re-resolving the offer at opening time is also what lets an
/// operator fix a mis-seeded bundle after it was already gifted.
/// <para>
/// Kept out of stuff data deliberately. Everything in stuff data reaches every client in the room,
/// so a present stored that way would announce its own contents before anybody opened it.
/// </para>
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record PresentContentsSnapshot
{
    [Id(0)]
    public required int OfferId { get; init; }

    [Id(1)]
    public required string ExtraParam { get; init; }

    /// <summary>Box and ribbon packed as <c>box * 1000 + ribbon</c>, which is how the client's
    /// gift-wrapped visualization reads the floor item's <c>extra</c> field.</summary>
    [Id(2)]
    public required int Wrapping { get; init; }
}
