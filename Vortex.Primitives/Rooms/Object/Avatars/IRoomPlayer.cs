using Vortex.Primitives.Orleans.Snapshots.Players;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object.Logic.Avatars;

namespace Vortex.Primitives.Rooms.Object.Avatars;

public interface IRoomPlayer : IRoomAvatar<IRoomPlayer, IRoomPlayerLogic, IRoomPlayerContext>
{
    new IRoomPlayerLogic Logic { get; }
    public PlayerId PlayerId { get; }
    public AvatarGenderType Gender { get; }
    public AvatarDanceType DanceType { get; }

    /// <summary>Guild whose badge the avatar displays, or -1 for none.</summary>
    public int GroupId { get; }
    public string GroupName { get; }
    public bool UpdateWithPlayer(PlayerSummarySnapshot snapshot);
    public bool SetDance(AvatarDanceType danceType = AvatarDanceType.None);

    /// <summary>
    /// Points the avatar's guild badge at <paramref name="groupId"/>; 0 or less clears it. Returns
    /// true only when something changed, so a no-op favourite toggle costs no room broadcast.
    /// </summary>
    public bool SetFavouriteGroup(int groupId, string groupName);
}
