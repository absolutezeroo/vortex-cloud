using Vortex.Primitives.Furniture;
using Vortex.Primitives.Furniture.Snapshots;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Object.Logic.Furniture;
using Vortex.Primitives.Rooms.Snapshots.Furniture;

namespace Vortex.Primitives.Rooms.Object.Furniture;

public interface IRoomItem<TSelf, out TLogic, out TContext>
    : IRoomObject<TSelf, TLogic, TContext>,
        IRoomItem
    where TSelf : IRoomItem<TSelf, TLogic, TContext>
    where TContext : IRoomItemContext<TSelf, TLogic, TContext>
    where TLogic : IFurnitureLogic<TSelf, TLogic, TContext>
{
    new TLogic Logic { get; }
}

public interface IRoomItem : IRoomObject
{
    new IFurnitureLogic Logic { get; }
    public PlayerId OwnerId { get; }
    public string OwnerName { get; }
    public Altitude Height { get; }
    public IExtraData ExtraData { get; }
    public FurnitureDefinitionSnapshot Definition { get; }
    public bool IsInvisible { get; }
    public void SetExtraData(string? extraData);
    public void SetOwnerId(PlayerId ownerId);
    public void SetOwnerName(string ownerName);
    public Altitude GetStackHeight();
    public RoomItemSnapshot GetSnapshot();
    public IComposer GetAddComposer();
    public IComposer GetUpdateComposer();
    public IComposer GetRefreshStuffDataComposer();
    public IComposer GetRemoveComposer(PlayerId pickerId, bool isExpired = false, int delay = 0);
}
