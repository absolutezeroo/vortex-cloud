using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Primitives.FriendList.Grains;
using Vortex.Primitives.Navigator;
using Vortex.Primitives.Navigator.Enums;
using Vortex.Primitives.Observability;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Navigator;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Primitives.Server.Grains;
using Vortex.Primitives.Snapshots.FriendList;
using Vortex.Primitives.Snapshots.Navigator;

namespace Vortex.Navigator;

public sealed class NavigatorService(
    ILogger<INavigatorService> logger,
    INavigatorProvider navigatorProvider,
    IRoomService roomService,
    IGrainFactory grainFactory,
    IVortexMetrics metrics
) : INavigatorService
{
    private readonly ILogger<INavigatorService> _logger = logger;
    private readonly INavigatorProvider _navigatorProvider = navigatorProvider;
    private readonly IRoomService _roomService = roomService;
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly IVortexMetrics _metrics = metrics;

    public async Task<ImmutableArray<NavigatorTopLevelContextSnapshot>> GetTopLevelContextAsync() =>
        await _navigatorProvider.GetTopLevelContextsAsync().ConfigureAwait(false);

    public ImmutableArray<NavigatorFlatCategorySnapshot> GetFlatCategories() =>
        _navigatorProvider.GetFlatCategories();

    public async Task<ImmutableArray<NavigatorSearchResultBlockSnapshot>> GetCategoryBlocksAsync(
        CancellationToken ct
    )
    {
        ImmutableArray<NavigatorFlatCategorySnapshot> categories =
            _navigatorProvider.GetFlatCategories();

        if (categories.Length == 0)
        {
            return [];
        }

        int limit = await _grainFactory
            .GetServerConfigGrain()
            .GetIntAsync(
                NavigatorConfig.CategoryResultLimitKey,
                NavigatorConfig.CategoryResultLimitDefault
            )
            .ConfigureAwait(false);

        // The per-category queries are independent, so issue them together rather than walking the
        // category list one round trip at a time.
        List<RoomInfoSnapshot>[] roomsPerCategory = await Task.WhenAll(
                categories.Select(cat =>
                    _navigatorProvider.GetRoomsByCategoryAsync(cat.Id, limit, ct)
                )
            )
            .ConfigureAwait(false);

        Dictionary<RoomId, RoomSummarySnapshot> activeById = await GetActiveRoomsByIdAsync()
            .ConfigureAwait(false);

        List<NavigatorSearchResultBlockSnapshot> blocks = new(categories.Length);

        for (int i = 0; i < categories.Length; i++)
        {
            blocks.Add(
                new NavigatorSearchResultBlockSnapshot
                {
                    SearchCode = NavigatorSearchCodes.Categories,
                    Text = ToCategoryHeader(categories[i]),
                    ActionAllowed = NavigatorActionAllowedType.Collapsed,
                    Localization = string.Empty,
                    ForceClosed = false,
                    ViewMode = NavigatorViewModeType.Rows,
                    Results = ToSearchResults(roomsPerCategory[i], activeById),
                }
            );
        }

        return [.. blocks];
    }

    public async Task<ImmutableArray<NavigatorSearchResultSnapshot>> GetSearchResultsAsync(
        string searchCode,
        NavigatorSearchFilterType filterType,
        string filterValue,
        PlayerId playerId,
        CancellationToken ct
    )
    {
        List<RoomInfoSnapshot> rooms = await FetchRoomsAsync(
                searchCode,
                filterType,
                filterValue,
                playerId,
                ct
            )
            .ConfigureAwait(false);

        if (rooms.Count == 0)
        {
            return [];
        }

        Dictionary<RoomId, RoomSummarySnapshot> activeById = await GetActiveRoomsByIdAsync()
            .ConfigureAwait(false);

        return ToSearchResults(rooms, activeById);
    }

    /// <summary>
    /// A block's header is whatever the server puts in <c>Text</c>, verbatim. The client's own flat
    /// category widget renders a category as <c>${navigator.flatcategory.global.&lt;KEY&gt;}</c>
    /// when it has a global key, so mirror that here instead of pushing the operator-facing English
    /// name at every locale.
    /// </summary>
    private static string ToCategoryHeader(NavigatorFlatCategorySnapshot category) =>
        string.IsNullOrWhiteSpace(category.GlobalCategory)
            ? category.Name
            : $"${{navigator.flatcategory.global.{category.GlobalCategory}}}";

    private async Task<Dictionary<RoomId, RoomSummarySnapshot>> GetActiveRoomsByIdAsync()
    {
        ImmutableArray<RoomSummarySnapshot> activeRooms;

        using (_metrics.MeasureRoomDirectoryCall(nameof(IRoomDirectoryGrain.GetActiveRoomsAsync)))
        {
            activeRooms = await _grainFactory
                .GetRoomDirectoryGrain()
                .GetActiveRoomsAsync()
                .ConfigureAwait(false);
        }

        return activeRooms.ToDictionary(x => x.RoomId);
    }

    /// <summary>Overlays live room state (name, owner, description, population) on the persisted
    /// row for any room that currently has an activated grain.</summary>
    private static ImmutableArray<NavigatorSearchResultSnapshot> ToSearchResults(
        List<RoomInfoSnapshot> rooms,
        Dictionary<RoomId, RoomSummarySnapshot> activeById
    ) =>
        [
            .. rooms.Select(x =>
            {
                RoomSummarySnapshot? active = activeById.TryGetValue(
                    x.RoomId,
                    out RoomSummarySnapshot? ar
                )
                    ? ar
                    : null;

                return new NavigatorSearchResultSnapshot
                {
                    RoomId = x.RoomId,
                    Name = active?.Name ?? x.Name,
                    OwnerId = active?.OwnerId ?? x.OwnerId,
                    OwnerName = active?.OwnerName ?? x.OwnerName,
                    DoorMode = x.DoorMode,
                    Population = active?.Population ?? 0,
                    PlayersMax = x.PlayersMax,
                    Description = active?.Description ?? x.Description,
                    TradeType = x.TradeType,
                    Score = x.Score,
                    Ranking = x.Ranking,
                    CategoryId = x.CategoryId,
                    Tags = x.Tags,
                    StaffPick = x.StaffPick,
                    AllowBlocking = x.AllowBlocking,
                    AllowPets = x.AllowPets,
                    AllowPetsEat = x.AllowPetsEat,
                    PaintWall = x.PaintWall,
                    PaintFloor = x.PaintFloor,
                    PaintLandscape = x.PaintLandscape,
                    // Drives RoomBitmaskFlags.GroupData in the navigator entry: the client only
                    // draws the guild badge when these three are present.
                    GroupId = x.GroupId,
                    GroupName = x.GroupName,
                    GroupBadge = x.GroupBadge,
                    LastUpdatedUtc = x.LastUpdatedUtc,
                };
            }),
        ];

    public async Task<ImmutableArray<OfficialRoomEntrySnapshot>> GetOfficialRoomEntriesAsync(
        CancellationToken ct
    )
    {
        int limit = await _grainFactory
            .GetServerConfigGrain()
            .GetIntAsync(
                NavigatorConfig.SearchResultLimitKey,
                NavigatorConfig.SearchResultLimitDefault
            )
            .ConfigureAwait(false);

        List<RoomInfoSnapshot> rooms = await _navigatorProvider
            .GetStaffPickedRoomsAsync(limit, ct)
            .ConfigureAwait(false);

        if (rooms.Count == 0)
        {
            return [];
        }

        Dictionary<RoomId, RoomSummarySnapshot> activeById = await GetActiveRoomsByIdAsync()
            .ConfigureAwait(false);

        // The hotel has no separate "official rooms tree" model, so the staff-pick flag is what
        // marks a room as public. Each one is emitted as a Room entry carrying its own room data,
        // which is what the client needs to open it directly from this view.
        List<OfficialRoomEntrySnapshot> entries = new(rooms.Count);

        for (int i = 0; i < rooms.Count; i++)
        {
            RoomInfoSnapshot room = rooms[i];
            RoomSummarySnapshot? active = activeById.GetValueOrDefault(room.RoomId);

            RoomInfoSnapshot live = room with
            {
                Name = active?.Name ?? room.Name,
                Description = active?.Description ?? room.Description,
                OwnerName = active?.OwnerName ?? room.OwnerName,
                Population = active?.Population ?? 0,
            };

            entries.Add(
                new OfficialRoomEntrySnapshot
                {
                    Index = i,
                    PopupCaption = live.Name,
                    PopupDescription = live.Description,
                    ShowDetails = true,
                    PictureText = string.Empty,
                    // No official-room artwork exists to point at; an empty ref makes the client
                    // skip the image rather than request a missing asset.
                    PictureRef = string.Empty,
                    FolderId = -1,
                    UserCount = live.Population,
                    Type = OfficialRoomEntryType.Room,
                    Room = live,
                }
            );
        }

        return [.. entries];
    }

    public async Task<CategoriesWithVisitorCountSnapshot> GetCategoryVisitorCountsAsync(
        CancellationToken ct
    )
    {
        ImmutableDictionary<int, NavigatorCategoryVisitorCount> counts = await _navigatorProvider
            .GetCategoryVisitorCountsAsync(ct)
            .ConfigureAwait(false);

        Dictionary<int, List<int>> byCategory = [];

        // Every visible category is reported, including the empty ones: a category the client never
        // hears about renders without a population instead of with a zero.
        foreach (NavigatorFlatCategorySnapshot category in _navigatorProvider.GetFlatCategories())
        {
            NavigatorCategoryVisitorCount count = counts.GetValueOrDefault(category.Id);

            byCategory[category.Id] = [count.CurrentUserCount, count.MaxUserCount];
        }

        return new CategoriesWithVisitorCountSnapshot(byCategory);
    }

    private async Task<List<RoomInfoSnapshot>> FetchRoomsAsync(
        string searchCode,
        NavigatorSearchFilterType filterType,
        string filterValue,
        PlayerId playerId,
        CancellationToken ct
    )
    {
        IServerConfigGrain config = _grainFactory.GetServerConfigGrain();

        int limit = await config
            .GetIntAsync(
                NavigatorConfig.SearchResultLimitKey,
                NavigatorConfig.SearchResultLimitDefault
            )
            .ConfigureAwait(false);

        // Explicit filter overrides searchCode routing
        if (
            !string.IsNullOrWhiteSpace(filterValue)
            && filterType != NavigatorSearchFilterType.Anything
        )
        {
            return filterType switch
            {
                NavigatorSearchFilterType.RoomName => await _navigatorProvider
                    .GetRoomsByNameAsync(filterValue, limit, ct)
                    .ConfigureAwait(false),
                NavigatorSearchFilterType.Owner => await _navigatorProvider
                    .GetRoomsByOwnerNameAsync(filterValue, limit, ct)
                    .ConfigureAwait(false),
                NavigatorSearchFilterType.Tag => await _navigatorProvider
                    .GetRoomsByTagAsync(filterValue, limit, ct)
                    .ConfigureAwait(false),
                NavigatorSearchFilterType.Group => await _navigatorProvider
                    .GetRoomsByGroupNameAsync(filterValue, limit, ct)
                    .ConfigureAwait(false),
                _ => await _navigatorProvider
                    .SearchRoomsAsync(filterValue, limit, ct)
                    .ConfigureAwait(false),
            };
        }

        NavigatorQueryType queryType = _navigatorProvider.ResolveQueryType(searchCode);

        // An unprefixed search box entry arrives as Anything + a value: it is a free-text search
        // whatever tab it was typed on.
        if (
            !string.IsNullOrWhiteSpace(filterValue)
            && queryType != NavigatorQueryType.ByFlatCategory
        )
        {
            return await _navigatorProvider
                .SearchRoomsAsync(filterValue, limit, ct)
                .ConfigureAwait(false);
        }

        return queryType switch
        {
            NavigatorQueryType.MyRooms => await _navigatorProvider
                .GetRoomsByOwnerAsync(playerId, limit, ct)
                .ConfigureAwait(false),
            NavigatorQueryType.MyFavorites => await _navigatorProvider
                .GetFavoriteRoomsAsync(playerId, limit, ct)
                .ConfigureAwait(false),
            NavigatorQueryType.FriendsRooms => await _navigatorProvider
                .GetFriendsRoomsAsync(playerId, limit, ct)
                .ConfigureAwait(false),
            NavigatorQueryType.WithFriends => await GetRoomsWithFriendsAsync(playerId, ct)
                .ConfigureAwait(false),
            NavigatorQueryType.History => await GetVisitedRoomsAsync(playerId, false, config, ct)
                .ConfigureAwait(false),
            NavigatorQueryType.FrequentHistory => await GetVisitedRoomsAsync(
                    playerId,
                    true,
                    config,
                    ct
                )
                .ConfigureAwait(false),
            NavigatorQueryType.WithRights => await _navigatorProvider
                .GetRoomsWithRightsAsync(playerId, limit, ct)
                .ConfigureAwait(false),
            NavigatorQueryType.RoomAds => await _navigatorProvider
                .GetAdvertisedRoomsAsync(limit, ct)
                .ConfigureAwait(false),
            NavigatorQueryType.ByFlatCategory => await GetCategoryRoomsAsync(
                    searchCode,
                    filterValue,
                    limit,
                    ct
                )
                .ConfigureAwait(false),
            NavigatorQueryType.EventCategory => await GetEventCategoryRoomsAsync(
                    searchCode,
                    limit,
                    ct
                )
                .ConfigureAwait(false),
            // "Popular" is the busiest-first ordering the default query already applies.
            NavigatorQueryType.Popular => await _navigatorProvider
                .GetAllRoomsAsync(limit, ct)
                .ConfigureAwait(false),
            NavigatorQueryType.HighestScore => await _navigatorProvider
                .GetHighestScoreRoomsAsync(limit, ct)
                .ConfigureAwait(false),
            NavigatorQueryType.Recommended => await _navigatorProvider
                .GetRecommendedRoomsAsync(playerId, limit, ct)
                .ConfigureAwait(false),
            NavigatorQueryType.StaffPicks => await _navigatorProvider
                .GetStaffPickedRoomsAsync(limit, ct)
                .ConfigureAwait(false),
            NavigatorQueryType.TextSearch => string.IsNullOrWhiteSpace(filterValue)
                ? []
                : await _navigatorProvider
                    .SearchRoomsAsync(filterValue, limit, ct)
                    .ConfigureAwait(false),
            NavigatorQueryType.MyGroups => await _navigatorProvider
                .GetMyGuildBaseRoomsAsync(playerId, limit, ct)
                .ConfigureAwait(false),
            NavigatorQueryType.GuildBases => await _navigatorProvider
                .GetGuildBaseRoomsAsync(limit, ct)
                .ConfigureAwait(false),
            // No competition subsystem exists. Empty is the honest answer; the fallback below would
            // present the entire hotel as competition entrants.
            NavigatorQueryType.Competition => [],
            _ => await _navigatorProvider.GetAllRoomsAsync(limit, ct).ConfigureAwait(false),
        };
    }

    private async Task<List<RoomInfoSnapshot>> GetVisitedRoomsAsync(
        PlayerId playerId,
        bool byFrequency,
        IServerConfigGrain config,
        CancellationToken ct
    )
    {
        int limit = await config
            .GetIntAsync(NavigatorConfig.HistoryLimitKey, NavigatorConfig.HistoryLimitDefault)
            .ConfigureAwait(false);

        return await _navigatorProvider
            .GetVisitedRoomsAsync(playerId, byFrequency, limit, ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// "Rooms where my friends are" is live state, not a query: the only place a player's current
    /// room is recorded is their presence grain. Ask every online friend's presence grain at once
    /// and turn the distinct room ids into room rows.
    /// </summary>
    private async Task<List<RoomInfoSnapshot>> GetRoomsWithFriendsAsync(
        PlayerId playerId,
        CancellationToken ct
    )
    {
        List<MessengerFriendSnapshot> friends = await _grainFactory
            .GetMessengerGrain(playerId)
            .GetFriendsAsync(ct)
            .ConfigureAwait(false);

        List<PlayerId> onlineFriendIds = [.. friends.Where(f => f.Online).Select(f => f.PlayerId)];

        if (onlineFriendIds.Count == 0)
        {
            return [];
        }

        RoomPointerSnapshot[] activeRooms = await Task.WhenAll(
                onlineFriendIds.Select(id =>
                    _grainFactory.GetPlayerPresenceGrain(id).GetActiveRoomAsync()
                )
            )
            .ConfigureAwait(false);

        HashSet<int> roomIds =
        [
            .. activeRooms.Where(r => r.RoomId > 0).Select(r => r.RoomId.Value),
        ];

        if (roomIds.Count == 0)
        {
            return [];
        }

        return await _navigatorProvider.GetRoomsByIdsAsync(roomIds, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// <c>eventcategory__3</c> means "rooms currently advertised under event category 3" -- the id
    /// travels in the search code itself, so there is one code per <c>navigator_eventcats</c> row.
    /// </summary>
    private async Task<List<RoomInfoSnapshot>> GetEventCategoryRoomsAsync(
        string searchCode,
        int limit,
        CancellationToken ct
    )
    {
        // Normally "eventcategory__3", but an operator can point any code at this query type, so
        // fall back to "whatever follows the last separator".
        string idPart = searchCode.StartsWith(
            NavigatorSearchCodes.EventCategoryPrefix,
            StringComparison.Ordinal
        )
            ? searchCode[NavigatorSearchCodes.EventCategoryPrefix.Length..]
            : searchCode[(searchCode.LastIndexOfAny(['_', ':']) + 1)..];

        if (
            !int.TryParse(
                idPart,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int eventCategoryId
            )
        )
        {
            _logger.LogDebug(
                "Navigator event-category search carried no usable id: SearchCode={SearchCode}",
                searchCode
            );

            return [];
        }

        return await _navigatorProvider
            .GetRoomsByEventCategoryAsync(eventCategoryId, limit, ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Resolves which flat category a category search means. This used to be hardcoded to id 0, so
    /// every category quick link returned the same (usually empty) block regardless of which
    /// category the player clicked.
    /// </summary>
    private async Task<List<RoomInfoSnapshot>> GetCategoryRoomsAsync(
        string searchCode,
        string filterValue,
        int limit,
        CancellationToken ct
    )
    {
        int? categoryId = ResolveFlatCategoryId(searchCode, filterValue);

        if (categoryId is null)
        {
            _logger.LogDebug(
                "Navigator category search resolved no category: SearchCode={SearchCode}, Filter={Filter}",
                searchCode,
                filterValue
            );

            return [];
        }

        return await _navigatorProvider
            .GetRoomsByCategoryAsync(categoryId.Value, limit, ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// A category is addressed either by id or by name, and it can arrive in the filtering data
    /// (<c>category:3</c>) or baked into the search code of a configured quick link
    /// (<c>category_3</c>, or simply the category's own name).
    /// </summary>
    private int? ResolveFlatCategoryId(string searchCode, string filterValue)
    {
        ImmutableArray<NavigatorFlatCategorySnapshot> categories =
            _navigatorProvider.GetFlatCategories();

        if (categories.Length == 0)
        {
            return null;
        }

        foreach (string candidate in EnumerateCategoryTokens(searchCode, filterValue))
        {
            if (
                int.TryParse(
                    candidate,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int id
                )
            )
            {
                if (categories.Any(c => c.Id == id))
                {
                    return id;
                }

                continue;
            }

            foreach (NavigatorFlatCategorySnapshot category in categories)
            {
                if (string.Equals(category.Name, candidate, StringComparison.OrdinalIgnoreCase))
                {
                    return category.Id;
                }
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateCategoryTokens(
        string searchCode,
        string filterValue
    )
    {
        if (!string.IsNullOrWhiteSpace(filterValue))
        {
            yield return filterValue.Trim();
        }

        if (string.IsNullOrWhiteSpace(searchCode))
        {
            yield break;
        }

        yield return searchCode.Trim();

        int separator = searchCode.LastIndexOfAny(['_', ':']);

        if (separator >= 0 && separator < searchCode.Length - 1)
        {
            yield return searchCode[(separator + 1)..];
        }
    }
}
