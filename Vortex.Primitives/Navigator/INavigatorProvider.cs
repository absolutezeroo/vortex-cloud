using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Navigator.Enums;
using Vortex.Primitives.Orleans.Snapshots.Navigator;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Players;

namespace Vortex.Primitives.Navigator;

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

    /// <summary>Most frequently used Tag1/Tag2 values across non-deleted rooms, most popular
    /// first -- backs GetPopularRoomTagsMessage.</summary>
    Task<ImmutableArray<string>> GetPopularTagsAsync(int limit, CancellationToken ct = default);

    /// <summary>Rooms currently promoted (staff pick or an active room advertisement), optionally
    /// narrowed to a flat category by name -- backs ForwardToARandomPromotedRoomMessage.</summary>
    Task<List<RoomInfoSnapshot>> GetPromotedRoomsAsync(
        string? categoryName,
        CancellationToken ct = default
    );

    /// <summary>The navigator_eventcats reference list -- backs GetUserEventCatsMessage.</summary>
    Task<ImmutableArray<NavigatorEventCategorySnapshot>> GetEventCategoriesAsync(
        CancellationToken ct = default
    );

    Task<List<RoomInfoSnapshot>> GetFavoriteRoomsAsync(
        PlayerId playerId,
        CancellationToken ct = default
    );

    /// <summary>Rooms with a currently non-expired RoomAdvertisementEntity -- backs
    /// NavigatorQueryType.RoomAds, the "sponsored rooms" navigator category.</summary>
    Task<List<RoomInfoSnapshot>> GetAdvertisedRoomsAsync(CancellationToken ct = default);

    Task ReloadAsync(CancellationToken ct = default);
}
