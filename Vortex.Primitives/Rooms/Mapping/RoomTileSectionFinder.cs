using System;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Rooms.Mapping;

/// <summary>
/// Which surfaces of a tile you can actually stand on, given what is stacked there.
///
/// Pure arithmetic over altitudes: no room, no grain, no items — the caller flattens its furniture
/// into <see cref="RoomTileOccupant" />s first. That is what makes the rule below testable at all,
/// and it is the rule that decides whether an avatar can walk *under* a raised platform, which is
/// the point of the exercise.
///
/// A tile offers one candidate surface per occupant top, plus the model's own floor. A candidate is
/// usable when two things hold:
///
///   nothing straddles it   an item with <c>Bottom &lt; S &lt; Top</c> passes through the surface,
///                          so there is no standing on it. The item *forming* S has
///                          <c>Top == S</c> and is therefore not straddling — that strict
///                          comparison is the whole distinction between "I am on it" and "I am
///                          inside it".
///   there is headroom      the nearest thing resting at or above S must be at least
///                          <see cref="Clearance" /> higher. An item sitting exactly on S gives
///                          zero headroom, which is correct: you do not stand under a chair, you
///                          stand on it, and that is the chair's own surface one candidate up.
/// </summary>
public static class RoomTileSectionFinder
{
    /// <summary>
    /// How much empty space an avatar needs above a surface to occupy it.
    ///
    /// Two, the same figure Skylight3 passes to its own gap search, and the same order as
    /// <c>RoomConfig.MaxStepHeight</c>. It is not derived from the avatar sprite — no server knows
    /// how tall a figure draws — it is the height Habbo's own rooms are built to.
    /// </summary>
    public static readonly Altitude Clearance = Altitude.FromValue(2);

    /// <summary>
    /// The best surface to step onto, coming from <paramref name="fromZ" />.
    ///
    /// "Best" is the highest usable surface within <paramref name="maxStep" /> of where the foot is
    /// now: Habbo prefers stepping up onto furniture over walking past it, and a tile with a rug on
    /// the floor should put you on the rug. Null means the tile offers nothing reachable — which is
    /// a legitimate answer for a cliff, and the one the pathfinder needs in order to route around
    /// it rather than into it.
    /// </summary>
    public static RoomTileSection? Find(
        Altitude floorHeight,
        ReadOnlySpan<RoomTileOccupant> occupants,
        Altitude fromZ,
        Altitude maxStep
    )
    {
        RoomTileSection? best = null;

        if (
            TryBuild(floorHeight, occupants, floorHeight, fromZ, maxStep, out RoomTileSection floor)
        )
        {
            best = floor;
        }

        foreach (RoomTileOccupant occupant in occupants)
        {
            if (
                !TryBuild(
                    floorHeight,
                    occupants,
                    occupant.Top,
                    fromZ,
                    maxStep,
                    out RoomTileSection section
                )
            )
            {
                continue;
            }

            if (best is null || section.Height > best.Value.Height)
            {
                best = section;
            }
        }

        return best;
    }

    /// <summary>
    /// The highest usable surface on the tile, ignoring where anyone is standing.
    ///
    /// This is the classic single answer — the one <c>ComputeTile()</c> derives and the client is
    /// told about in its height map — expressed through the same rule as everything else, so the
    /// two cannot drift apart unnoticed.
    /// </summary>
    public static RoomTileSection? FindTop(
        Altitude floorHeight,
        ReadOnlySpan<RoomTileOccupant> occupants
    ) => Find(floorHeight, occupants, floorHeight, Altitude.FromValue(double.MaxValue));

    private static bool TryBuild(
        Altitude floorHeight,
        ReadOnlySpan<RoomTileOccupant> occupants,
        Altitude surface,
        Altitude fromZ,
        Altitude maxStep,
        out RoomTileSection section
    )
    {
        section = default;

        if (surface < floorHeight || Math.Abs(surface - fromZ) > maxStep)
        {
            return false;
        }

        Altitude headroom = Altitude.FromValue(double.MaxValue);
        RoomTileOccupant? forming = null;

        foreach (RoomTileOccupant occupant in occupants)
        {
            if (occupant.Bottom < surface && occupant.Top > surface)
            {
                return false;
            }

            // Only what is genuinely overhead counts against the headroom: an occupant whose top is
            // at or below the surface is under the foot, not over the head. Without that test a rug
            // — top and bottom both on the floor it lies on — would block the very surface it
            // forms, and a tile with a rug on it would report nowhere to stand.
            if (occupant.Top > surface && occupant.Bottom - surface < headroom)
            {
                headroom = occupant.Bottom - surface;
            }

            // The forming item is the one whose top *is* this surface. Two items can share a top —
            // a pair of rugs at the same height — and the one resting highest is the one you are
            // actually standing on, so its flags are the ones that count.
            if (
                occupant.Top == surface
                && (forming is null || occupant.Bottom > forming.Value.Bottom)
            )
            {
                forming = occupant;
            }
        }

        if (headroom < Clearance)
        {
            return false;
        }

        section = new RoomTileSection
        {
            Height = surface,
            ItemId = forming?.ItemId ?? -1,
            Flags = forming?.Flags ?? RoomTileFlags.None,
        };

        return true;
    }
}
