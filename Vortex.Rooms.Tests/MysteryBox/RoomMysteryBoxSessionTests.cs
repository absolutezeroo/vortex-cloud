using FluentAssertions;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Object;
using Vortex.Rooms.Grains;
using Xunit;

namespace Vortex.Rooms.Tests.MysteryBox;

/// <summary>
/// Locks the pairing rules the <c>RoomGrain</c> reads before it spends a key and destroys a box: a
/// box opens only once both halves are present, and a session with neither half left is dead rather
/// than reserved. Getting either wrong either hands out prizes for one half or leaves a box
/// permanently claimed by someone who walked away.
/// </summary>
public sealed class RoomMysteryBoxSessionTests
{
    private static readonly PlayerId Owner = new(101);
    private static readonly PlayerId KeyHolder = new(202);
    private static readonly PlayerId Bystander = new(303);

    private static RoomMysteryBoxSession NewSession() =>
        new()
        {
            BoxObjectId = new RoomObjectId(7),
            BoxOwnerId = Owner,
            Color = "purple",
            StartedAtMs = 1000,
        };

    [Fact]
    public void FreshSession_IsNeitherReadyNorAbandoned_UntilSomeoneJoins()
    {
        RoomMysteryBoxSession session = NewSession();

        session.IsReadyToOpen.Should().BeFalse();
        session.IsAbandoned.Should().BeTrue();
    }

    [Fact]
    public void OwnerAlone_IsNotReadyToOpen()
    {
        RoomMysteryBoxSession session = NewSession();

        session.OwnerWaiting = true;

        session.IsReadyToOpen.Should().BeFalse();
        session.IsAbandoned.Should().BeFalse();
    }

    [Fact]
    public void KeyHolderAlone_IsNotReadyToOpen()
    {
        RoomMysteryBoxSession session = NewSession();

        session.KeyHolderId = KeyHolder;

        session.IsReadyToOpen.Should().BeFalse();
        session.IsAbandoned.Should().BeFalse();
    }

    [Fact]
    public void BothHalves_MakeItReadyToOpen()
    {
        RoomMysteryBoxSession session = NewSession();

        session.OwnerWaiting = true;
        session.KeyHolderId = KeyHolder;

        session.IsReadyToOpen.Should().BeTrue();
    }

    [Fact]
    public void Owner_IsOnlyAParticipantOnceTheyHaveClicked()
    {
        RoomMysteryBoxSession session = NewSession();

        // Owning the box is not the same as waiting on it: a cancel from an owner who never opened
        // the dialog must not tear down the key holder's wait.
        session.KeyHolderId = KeyHolder;
        session.IsParticipant(Owner).Should().BeFalse();

        session.OwnerWaiting = true;
        session.IsParticipant(Owner).Should().BeTrue();
    }

    [Fact]
    public void Bystanders_AreNeverParticipants()
    {
        RoomMysteryBoxSession session = NewSession();

        session.OwnerWaiting = true;
        session.KeyHolderId = KeyHolder;

        session.IsParticipant(Bystander).Should().BeFalse();
    }

    [Fact]
    public void ReleasingTheKeyHolder_LeavesTheOwnerWaiting()
    {
        RoomMysteryBoxSession session = NewSession();

        session.OwnerWaiting = true;
        session.KeyHolderId = KeyHolder;

        session.KeyHolderId = null;

        session.IsReadyToOpen.Should().BeFalse();
        session.IsAbandoned.Should().BeFalse();
        session.IsParticipant(Owner).Should().BeTrue();
    }

    [Fact]
    public void LosingBothHalves_AbandonsTheSession()
    {
        RoomMysteryBoxSession session = NewSession();

        session.OwnerWaiting = true;
        session.KeyHolderId = KeyHolder;

        session.OwnerWaiting = false;
        session.KeyHolderId = null;

        session.IsAbandoned.Should().BeTrue();
    }
}
