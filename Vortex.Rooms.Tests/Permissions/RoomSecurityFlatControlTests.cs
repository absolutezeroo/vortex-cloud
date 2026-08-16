using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Rooms.Object.Avatars.Player;
using Vortex.Rooms.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Permissions;

/// <summary>
/// The <c>flatctrl</c> avatar status is how every *other* client in the room learns that somebody
/// holds rights — the presence notification only redraws the subject's own UI. It used to be
/// stamped once when the avatar spawned, so a mid-session grant stayed invisible until the player
/// left and came back.
/// </summary>
public class RoomSecurityFlatControlTests
{
    [Fact]
    public async Task RefreshingControllerLevel_StampsThePlainVisitorAsHavingNoControl()
    {
        RoomHarness harness = await RoomHarness
            .CreateAsync(canManipulate: false)
            .ConfigureAwait(true);

        RoomPlayerAvatar avatar = harness.PutRealPlayerInRoom(RoomHarness.Stranger, 1, 1);

        await harness
            .Grain.SecurityModule.RefreshControllerLevelForPlayerAsync(
                RoomHarness.Stranger,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        avatar
            .Statuses[AvatarStatusType.FlatControl]
            .Should()
            .Be(((int)RoomControllerType.None).ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task GrantingRightsMidSession_RaisesTheAvatarsFlatControlWithoutRejoining()
    {
        RoomHarness harness = await RoomHarness
            .CreateAsync(canManipulate: false)
            .ConfigureAwait(true);

        RoomPlayerAvatar avatar = harness.PutRealPlayerInRoom(RoomHarness.Stranger, 1, 1);

        // What AssignRightsAsync does to live state before it asks for a refresh.
        harness.Grain._state.PlayerIdsWithRights.Add(RoomHarness.Stranger);

        await harness
            .Grain.SecurityModule.RefreshControllerLevelForPlayerAsync(
                RoomHarness.Stranger,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        avatar
            .Statuses[AvatarStatusType.FlatControl]
            .Should()
            .Be(((int)RoomControllerType.Rights).ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task RevokingRightsMidSession_DropsTheAvatarBackToNoControl()
    {
        RoomHarness harness = await RoomHarness
            .CreateAsync(canManipulate: true)
            .ConfigureAwait(true);

        RoomPlayerAvatar avatar = harness.PutRealPlayerInRoom(RoomHarness.Stranger, 1, 1);

        await harness
            .Grain.SecurityModule.RefreshControllerLevelForPlayerAsync(
                RoomHarness.Stranger,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        harness.Grain._state.PlayerIdsWithRights.Remove(RoomHarness.Stranger);

        await harness
            .Grain.SecurityModule.RefreshControllerLevelForPlayerAsync(
                RoomHarness.Stranger,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        avatar
            .Statuses[AvatarStatusType.FlatControl]
            .Should()
            .Be(((int)RoomControllerType.None).ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// A player who is not standing in the room has no avatar to stamp. The refresh still has to
    /// notify their presence grain rather than fall over on the lookup.
    /// </summary>
    [Fact]
    public async Task RefreshingForSomebodyNotInTheRoom_DoesNotThrow()
    {
        RoomHarness harness = await RoomHarness
            .CreateAsync(canManipulate: false)
            .ConfigureAwait(true);

        await harness
            .Grain.SecurityModule.RefreshControllerLevelForPlayerAsync(
                RoomHarness.Stranger,
                CancellationToken.None
            )
            .ConfigureAwait(true);
    }
}
