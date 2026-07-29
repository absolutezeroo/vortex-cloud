using FluentAssertions;
using Vortex.Primitives.Navigator.Enums;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Rooms.Enums;
using Xunit;

namespace Vortex.Rooms.Tests.Permissions;

/// <summary>
/// These settings were persisted and echoed to the client but enforced nowhere, so any occupant
/// could mute, kick and ban anyone. The two guild-scoped options were unreachable for the same
/// reason guild rights were.
/// </summary>
public sealed class RoomModerationPolicyTests
{
    [Theory]
    [InlineData(ModSettingType.Owner)]
    [InlineData(ModSettingType.Rights)]
    [InlineData(ModSettingType.GroupRights)]
    [InlineData(ModSettingType.RightsOrGroup)]
    public void Owner_MayModerate_UnderEverySetting(ModSettingType setting)
    {
        RoomModerationPolicy.CanModerate(RoomControllerType.Owner, setting).Should().BeTrue();
    }

    [Theory]
    [InlineData(ModSettingType.Owner)]
    [InlineData(ModSettingType.Rights)]
    [InlineData(ModSettingType.GroupRights)]
    [InlineData(ModSettingType.RightsOrGroup)]
    public void Moderator_MayModerate_UnderEverySetting(ModSettingType setting)
    {
        RoomModerationPolicy.CanModerate(RoomControllerType.Moderator, setting).Should().BeTrue();
    }

    [Theory]
    [InlineData(RoomControllerType.None)]
    [InlineData(RoomControllerType.Rights)]
    [InlineData(RoomControllerType.GroupRights)]
    [InlineData(RoomControllerType.GroupAdmin)]
    public void OwnerOnlySetting_RefusesEveryoneBelowOwner(RoomControllerType level)
    {
        RoomModerationPolicy.CanModerate(level, ModSettingType.Owner).Should().BeFalse();
    }

    [Fact]
    public void RightsSetting_AllowsRightsHolder()
    {
        RoomModerationPolicy
            .CanModerate(RoomControllerType.Rights, ModSettingType.Rights)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void RightsSetting_RefusesPlainVisitor()
    {
        RoomModerationPolicy
            .CanModerate(RoomControllerType.None, ModSettingType.Rights)
            .Should()
            .BeFalse();
    }

    /// <summary>"Guild rights" deliberately excludes a classic rights-holder.</summary>
    [Fact]
    public void GroupRightsSetting_RefusesPlainRightsHolder()
    {
        RoomModerationPolicy
            .CanModerate(RoomControllerType.Rights, ModSettingType.GroupRights)
            .Should()
            .BeFalse();
    }

    [Theory]
    [InlineData(RoomControllerType.GroupRights)]
    [InlineData(RoomControllerType.GroupAdmin)]
    public void GroupRightsSetting_AllowsGuildStanding(RoomControllerType level)
    {
        RoomModerationPolicy.CanModerate(level, ModSettingType.GroupRights).Should().BeTrue();
    }

    [Theory]
    [InlineData(RoomControllerType.Rights)]
    [InlineData(RoomControllerType.GroupRights)]
    [InlineData(RoomControllerType.GroupAdmin)]
    public void RightsOrGroupSetting_AllowsEitherSource(RoomControllerType level)
    {
        RoomModerationPolicy.CanModerate(level, ModSettingType.RightsOrGroup).Should().BeTrue();
    }

    /// <summary>
    /// Staff moderation authority is orthogonal to the controller ladder. A role holding
    /// <c>moderation.kick</c> but not <c>room.moderate.any</c> resolves to
    /// <see cref="RoomControllerType.None"/> in the room — it must still moderate, without that
    /// elevation also handing it build rights everywhere.
    /// </summary>
    [Theory]
    [InlineData(ModSettingType.Owner)]
    [InlineData(ModSettingType.Rights)]
    [InlineData(ModSettingType.GroupRights)]
    [InlineData(ModSettingType.RightsOrGroup)]
    public void StaffCapability_ModeratesRegardlessOfLadderAndSetting(ModSettingType setting)
    {
        RoomModerationPolicy
            .CanModerate(RoomControllerType.None, setting, hasStaffModerationCapability: true)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void WithoutStaffCapability_TheLadderStillDecides()
    {
        RoomModerationPolicy
            .CanModerate(RoomControllerType.None, ModSettingType.Rights, false)
            .Should()
            .BeFalse();

        RoomModerationPolicy
            .CanModerate(RoomControllerType.Rights, ModSettingType.Rights, false)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void RightsOrGroupSetting_RefusesPlainVisitor()
    {
        RoomModerationPolicy
            .CanModerate(RoomControllerType.None, ModSettingType.RightsOrGroup)
            .Should()
            .BeFalse();
    }
}
