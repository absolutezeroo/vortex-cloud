using System.Collections.Generic;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object.Logic.Avatars;
using Vortex.Primitives.Rooms.Snapshots.Avatars;

namespace Vortex.Primitives.Rooms.Object.Avatars;

public interface IRoomAvatar<TSelf, out TLogic, out TContext>
    : IRoomObject<TSelf, TLogic, TContext>,
        IRoomAvatar
    where TSelf : IRoomAvatar<TSelf, TLogic, TContext>
    where TContext : IRoomAvatarContext<TSelf, TLogic, TContext>
    where TLogic : IRoomAvatarLogic<TSelf, TLogic, TContext>
{
    new TLogic Logic { get; }
}

public interface IRoomAvatar : IRoomObject
{
    new IRoomAvatarLogic Logic { get; }
    public RoomObjectType AvatarType { get; }
    public string Name { get; }
    public string Motto { get; }
    public string Figure { get; }
    public Rotation HeadRotation { get; }
    public Dictionary<AvatarStatusType, string> Statuses { get; }

    public Altitude PostureOffset { get; set; }
    public int GoalTileId { get; }
    public int NextTileId { get; set; }
    public bool IsWalking { get; set; }
    public bool NeedsInvoke { get; set; }
    public List<int> TilePath { get; }

    public long NextMoveStepAtMs { get; set; }
    public long NextMoveUpdateAtMs { get; set; }
    public long PendingStopAtMs { get; set; }

    public int LastChatStyleId { get; set; }

    /// <summary>The avatar's currently worn effect id (0 = none). Sourced from the effect inventory (worn
    /// selection), wired give-effect, or a game team aura; broadcast via <c>AvatarEffectMessageComposer</c>
    /// and re-synced to late joiners at room entry.</summary>
    public int CurrentEffectId { get; }

    public bool SetGoalTileId(int tileId);
    public bool SetEffect(int effectId);
    public void SetHeight(Altitude z);
    public void SetBodyRotation(Rotation rot);
    public void SetHeadRotation(Rotation rot);
    public void Sit(bool flag = true, Altitude? height = null, Rotation? rot = null);
    public void Lay(bool flag = true, Altitude? height = null, Rotation? rot = null);

    public void AddStatus(AvatarStatusType type, string value);
    public bool HasStatus(params AvatarStatusType[] types);
    public void RemoveStatus(params AvatarStatusType[] types);

    public RoomAvatarSnapshot GetSnapshot();
}
