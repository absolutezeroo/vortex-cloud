using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Rooms.Grains;

/// <summary>
/// A mystery box waiting to be opened. Both halves have to click the same box, and neither the box
/// owner nor the key holder can be trusted to arrive first, so the session records whichever side
/// showed up and completes when the other one does.
///
/// Keyed by box owner rather than by furniture: the client's cancel message carries only the
/// furniture's owner id (<c>MysteryBoxWaitingCanceledMessageComposer(getFurnitureOwnerId(object))</c>),
/// so an owner cannot have two of these running at once even if two of their boxes are on the floor.
/// </summary>
internal sealed class RoomMysteryBoxSession
{
    public required RoomObjectId BoxObjectId { get; init; }
    public required PlayerId BoxOwnerId { get; init; }

    /// <summary>The colour the key must match. Comes from the box's furniture definition.</summary>
    public required string Color { get; init; }

    public required long StartedAtMs { get; set; }

    /// <summary>True once the owner has clicked their own box.</summary>
    public bool OwnerWaiting { get; set; }

    /// <summary>The first key holder to click the box; the session is reserved for them until they
    /// cancel or it times out.</summary>
    public PlayerId? KeyHolderId { get; set; }

    public bool IsReadyToOpen => OwnerWaiting && KeyHolderId is not null;

    public bool IsAbandoned => !OwnerWaiting && KeyHolderId is null;

    public bool IsParticipant(PlayerId playerId) =>
        (OwnerWaiting && BoxOwnerId == playerId) || KeyHolderId == playerId;
}
