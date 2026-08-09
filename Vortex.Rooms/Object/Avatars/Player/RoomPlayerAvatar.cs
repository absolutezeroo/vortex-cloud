using System.Text;
using Vortex.Primitives.Groups.Enums;
using Vortex.Primitives.Orleans.Snapshots.Players;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Logic.Avatars;
using Vortex.Primitives.Rooms.Snapshots.Avatars;

namespace Vortex.Rooms.Object.Avatars.Player;

public sealed class RoomPlayerAvatar
    : RoomAvatar<IRoomPlayer, IRoomPlayerLogic, IRoomPlayerContext>,
        IRoomPlayer
{
    /// <summary>The client's "no guild badge" sentinel.</summary>
    private const int NoGroup = -1;

    public int GroupId { get; private set; } = NoGroup;
    public int GroupStatus { get; private set; } = NoGroup;
    public string GroupName { get; private set; } = string.Empty;
    public string SwimFigure { get; init; } = string.Empty;
    public int ActivityPoints { get; init; } = 0;
    public bool IsModerator { get; init; } = false;
    public override RoomObjectType AvatarType { get; } = RoomObjectType.Player;

    public required PlayerId PlayerId { get; init; }
    public AvatarGenderType Gender { get; private set; } = AvatarGenderType.Male;
    public AvatarDanceType DanceType { get; private set; } = AvatarDanceType.None;

    public bool UpdateWithPlayer(PlayerSummarySnapshot snapshot)
    {
        Name = snapshot.Name;
        Motto = snapshot.Motto;
        Figure = snapshot.Figure;
        Gender = snapshot.Gender;

        SetFavouriteGroup(snapshot.FavouriteGroupId, snapshot.FavouriteGroupName);

        return true;
    }

    /// <summary>
    /// Points the avatar's guild badge at <paramref name="groupId"/> (0 or less clears it). Returns
    /// true when something actually changed, so callers can skip a pointless room broadcast.
    /// </summary>
    public bool SetFavouriteGroup(int groupId, string groupName)
    {
        int resolvedId = groupId > 0 ? groupId : NoGroup;
        int resolvedStatus = groupId > 0 ? (int)GroupMembershipStatus.Member : NoGroup;
        string resolvedName = groupId > 0 ? groupName : string.Empty;

        if (GroupId == resolvedId && GroupStatus == resolvedStatus && GroupName == resolvedName)
        {
            return false;
        }

        GroupId = resolvedId;
        GroupStatus = resolvedStatus;
        GroupName = resolvedName;

        _snapshot = null;

        return true;
    }

    /// <summary>Sitting cancels the dance, like it does on Habbo — the dance is dropped before the
    /// status goes on so <see cref="SetDance"/> isn't blocked by its own sit guard.</summary>
    public override void Sit(bool flag = true, Altitude? height = null, Rotation? rot = null)
    {
        if (flag)
        {
            SetDance(AvatarDanceType.None);
        }

        base.Sit(flag, height, rot);
    }

    /// <inheritdoc cref="Sit"/>
    public override void Lay(bool flag = true, Altitude? height = null, Rotation? rot = null)
    {
        if (flag)
        {
            SetDance(AvatarDanceType.None);
        }

        base.Lay(flag, height, rot);
    }

    public bool SetDance(AvatarDanceType danceType = AvatarDanceType.None)
    {
        if (DanceType == danceType)
        {
            return false;
        }

        if (HasStatus(AvatarStatusType.Sit, AvatarStatusType.Lay))
        {
            return false;
        }

        // check if dance valid
        // check if dance is hc only / validate hc

        DanceType = danceType;

        _snapshot = null;

        return true;
    }

    protected override RoomPlayerAvatarSnapshot BuildSnapshot()
    {
        StringBuilder statusString = new("/");

        foreach ((AvatarStatusType type, string value) in Statuses)
        {
            statusString.Append($"{type.ToLegacyString()} {value}/");
        }

        return new RoomPlayerAvatarSnapshot
        {
            AvatarType = AvatarType,
            WebId = PlayerId.Value,
            Name = Name,
            Motto = Motto,
            Figure = Figure,
            ObjectId = ObjectId,
            X = X,
            Y = Y,
            Z = Z,
            BodyRotation = Rotation,
            HeadRotation = HeadRotation,
            Status = statusString.ToString(),
            Gender = Gender,
            DanceType = DanceType,
            GroupId = GroupId,
            GroupStatus = GroupStatus,
            GroupName = GroupName,
            SwimFigure = SwimFigure,
            ActivityPoints = ActivityPoints,
            IsModerator = IsModerator,
            CurrentEffectId = CurrentEffectId,
            CarryItemId = CarryItemId,
        };
    }
}
