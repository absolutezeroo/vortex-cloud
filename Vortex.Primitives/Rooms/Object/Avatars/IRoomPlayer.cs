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
    public bool UpdateWithPlayer(PlayerSummarySnapshot snapshot);
    public bool SetDance(AvatarDanceType danceType = AvatarDanceType.None);
}
