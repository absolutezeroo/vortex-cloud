using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Furniture.Snapshots;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Object.Logic.Furniture;
using Vortex.Primitives.Rooms.Snapshots.Furniture;

namespace Vortex.Primitives.Rooms.Object.Furniture;

public interface IRoomItemContext<out TObject, out TLogic, out TSelf>
    : IRoomObjectContext<TObject, TLogic, TSelf>,
        IRoomItemContext
    where TObject : IRoomItem<TObject, TLogic, TSelf>
    where TSelf : IRoomItemContext<TObject, TLogic, TSelf>
    where TLogic : IFurnitureLogic<TObject, TLogic, TSelf>
{
    new TObject RoomObject { get; }
}

public interface IRoomItemContext : IRoomObjectContext
{
    new IRoomItem RoomObject { get; }
    public FurnitureDefinitionSnapshot Definition { get; }
    public Task<RoomFloorItemSnapshot?> GetFloorItemSnapshotByIdAsync(
        RoomObjectId objectId,
        CancellationToken ct
    );
    public Task AddItemAsync();
    public Task UpdateItemAsync();
    public Task RefreshStuffDataAsync();
    public Task RemoveItemAsync(PlayerId pickerId, bool isExpired = false, int delay = 0);
}
