using FluentAssertions;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Rooms.Object.Avatars.Player;
using Xunit;

namespace Vortex.Rooms.Tests.Avatars;

/// <summary>
/// A dance and a sit/lay posture cannot coexist on Habbo: taking a seat ends the dance. The avatar
/// used to keep its dance type while sitting, which left it dancing again the moment it stood back
/// up, so the posture change has to drop the dance on the avatar itself -- every caller (the posture
/// packet, walking onto a chair) goes through <c>Sit</c>/<c>Lay</c>.
/// </summary>
public sealed class RoomPlayerAvatarPostureTests
{
    private static RoomPlayerAvatar CreateAvatar() =>
        new() { ObjectId = new RoomObjectId(1), PlayerId = new PlayerId(42) };

    [Fact]
    public void Sitting_CancelsTheDance()
    {
        RoomPlayerAvatar avatar = CreateAvatar();
        avatar.SetDance(AvatarDanceType.PogoMogo).Should().BeTrue();

        avatar.Sit(true);

        avatar.DanceType.Should().Be(AvatarDanceType.None);
        avatar.HasStatus(AvatarStatusType.Sit).Should().BeTrue();
    }

    [Fact]
    public void Laying_CancelsTheDance()
    {
        RoomPlayerAvatar avatar = CreateAvatar();
        avatar.SetDance(AvatarDanceType.TheRollie).Should().BeTrue();

        avatar.Lay(true);

        avatar.DanceType.Should().Be(AvatarDanceType.None);
        avatar.HasStatus(AvatarStatusType.Lay).Should().BeTrue();
    }

    [Fact]
    public void StandingBackUp_DoesNotBringTheDanceBack()
    {
        RoomPlayerAvatar avatar = CreateAvatar();
        avatar.SetDance(AvatarDanceType.Dance);
        avatar.Sit(true);

        avatar.Sit(false);

        avatar.HasStatus(AvatarStatusType.Sit).Should().BeFalse();
        avatar.DanceType.Should().Be(AvatarDanceType.None);
    }

    [Fact]
    public void DancingWhileSeated_IsStillRefused()
    {
        RoomPlayerAvatar avatar = CreateAvatar();
        avatar.Sit(true);

        avatar.SetDance(AvatarDanceType.Dance).Should().BeFalse();
        avatar.DanceType.Should().Be(AvatarDanceType.None);
    }
}
