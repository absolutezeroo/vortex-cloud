namespace Vortex.Primitives.Events;

/// <summary>A mystery box key was handed to a player (staff grant, quest reward, refund).</summary>
public sealed record MysteryBoxKeyGrantedEvent(int PlayerId, string Color, string Source) : IEvent;

/// <summary>A key was spent. Emitted before the prizes are drawn, so a spend is on the trail even if
/// the award that follows fails.</summary>
public sealed record MysteryBoxKeyConsumedEvent(int PlayerId, string Color) : IEvent;

/// <summary>A box and a matching key were opened together. Both participants are named because a
/// mystery box is the one item whose consumption is triggered by someone other than its owner.</summary>
public sealed record MysteryBoxOpenedEvent(
    int RoomId,
    int BoxItemId,
    int BoxOwnerPlayerId,
    int KeyHolderPlayerId,
    string Color
) : IEvent;

// What each participant won is not here: prizes are drawn from shared pools, so the payout is
// recorded once by PrizeAwardedEvent for every reward furniture rather than once per furniture type.

/// <summary>A mystery trophy was inscribed and exchanged for a real trophy.</summary>
public sealed record MysteryTrophyOpenedEvent(
    int RoomId,
    int TrophyItemId,
    int PlayerId,
    int PrizeId,
    int PrizeFurnitureDefinitionId
) : IEvent;
