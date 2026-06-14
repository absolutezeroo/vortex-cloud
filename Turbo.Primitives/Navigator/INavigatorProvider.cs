using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Turbo.Primitives.Navigator.Enums;
using Turbo.Primitives.Orleans.Snapshots.Navigator;
using Turbo.Primitives.Orleans.Snapshots.Room;
using Turbo.Primitives.Players;

namespace Turbo.Primitives.Navigator;

public interface INavigatorProvider
{
    Task<ImmutableArray<NavigatorTopLevelContextSnapshot>> GetTopLevelContextsAsync();

    NavigatorQueryType ResolveQueryType(string searchCode);

    ImmutableArray<NavigatorFlatCategorySnapshot> GetFlatCategories();

    Task<List<RoomInfoSnapshot>> GetAllRoomsAsync(CancellationToken ct = default);

    Task<List<RoomInfoSnapshot>> GetRoomsByOwnerAsync(
        PlayerId playerId,
        CancellationToken ct = default
    );

    Task<List<RoomInfoSnapshot>> GetRoomsByCategoryAsync(
        int categoryId,
        CancellationToken ct = default
    );

    Task<List<RoomInfoSnapshot>> GetRoomsByNameAsync(string name, CancellationToken ct = default);

    Task<List<RoomInfoSnapshot>> GetRoomsByOwnerNameAsync(
        string ownerName,
        CancellationToken ct = default
    );

    Task<List<RoomInfoSnapshot>> GetRoomsByTagAsync(string tag, CancellationToken ct = default);

    Task<List<RoomInfoSnapshot>> GetFavoriteRoomsAsync(
        PlayerId playerId,
        CancellationToken ct = default
    );

    Task ReloadAsync(CancellationToken ct = default);
}
