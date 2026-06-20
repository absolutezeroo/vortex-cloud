using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Turbo.Primitives.Navigator;
using Turbo.Primitives.Navigator.Enums;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Orleans.Snapshots.Navigator;
using Turbo.Primitives.Orleans.Snapshots.Room;
using Turbo.Primitives.Players;
using Turbo.Primitives.Rooms;

namespace Turbo.Navigator;

public sealed class NavigatorService(
    ILogger<INavigatorService> logger,
    INavigatorProvider navigatorProvider,
    IRoomService roomService,
    IGrainFactory grainFactory
) : INavigatorService
{
    private readonly ILogger<INavigatorService> _logger = logger;
    private readonly INavigatorProvider _navigatorProvider = navigatorProvider;
    private readonly IRoomService _roomService = roomService;
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async Task<ImmutableArray<NavigatorTopLevelContextSnapshot>> GetTopLevelContextAsync() =>
        await _navigatorProvider.GetTopLevelContextsAsync().ConfigureAwait(false);

    public ImmutableArray<NavigatorFlatCategorySnapshot> GetFlatCategories() =>
        _navigatorProvider.GetFlatCategories();

    public async Task<ImmutableArray<NavigatorSearchResultBlockSnapshot>> GetCategoryBlocksAsync(
        CancellationToken ct
    )
    {
        ImmutableArray<NavigatorFlatCategorySnapshot> categories = _navigatorProvider.GetFlatCategories();
        ImmutableArray<RoomSummarySnapshot> activeRooms = await _grainFactory
            .GetRoomDirectoryGrain()
            .GetActiveRoomsAsync()
            .ConfigureAwait(false);
        Dictionary<RoomId, RoomSummarySnapshot> activeById = activeRooms.ToDictionary(x => x.RoomId);

        List<NavigatorSearchResultBlockSnapshot> blocks = new List<NavigatorSearchResultBlockSnapshot>(categories.Length);

        foreach (NavigatorFlatCategorySnapshot cat in categories)
        {
            List<RoomInfoSnapshot> rooms = await _navigatorProvider
                .GetRoomsByCategoryAsync(cat.Id, ct)
                .ConfigureAwait(false);

            blocks.Add(
                new NavigatorSearchResultBlockSnapshot
                {
                    SearchCode = "categories",
                    Text = cat.Name,
                    ActionAllowed = NavigatorActionAllowedType.Collapsed,
                    Localization = string.Empty,
                    ForceClosed = false,
                    ViewMode = NavigatorViewModeType.Rows,
                    Results =
                    [
                        .. rooms.Select(x =>
                        {
                            RoomSummarySnapshot? active = activeById.TryGetValue(x.RoomId, out RoomSummarySnapshot? ar) ? ar : null;
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
                                AllowBlocking = x.AllowBlocking,
                                AllowPets = x.AllowPets,
                                AllowPetsEat = x.AllowPetsEat,
                                LastUpdatedUtc = x.LastUpdatedUtc,
                            };
                        }),
                    ],
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
        List<RoomInfoSnapshot> rooms = await FetchRoomsAsync(searchCode, filterType, filterValue, playerId, ct)
            .ConfigureAwait(false);

        ImmutableArray<RoomSummarySnapshot> activeRooms = await _grainFactory
            .GetRoomDirectoryGrain()
            .GetActiveRoomsAsync()
            .ConfigureAwait(false);

        Dictionary<RoomId, RoomSummarySnapshot> activeById = activeRooms.ToDictionary(x => x.RoomId);

        return
        [
            .. rooms.Select(x =>
            {
                RoomSummarySnapshot? active = activeById.TryGetValue(x.RoomId, out RoomSummarySnapshot? ar) ? ar : null;

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
                    AllowBlocking = x.AllowBlocking,
                    AllowPets = x.AllowPets,
                    AllowPetsEat = x.AllowPetsEat,
                    LastUpdatedUtc = x.LastUpdatedUtc,
                };
            }),
        ];
    }

    private async Task<System.Collections.Generic.List<Primitives.Orleans.Snapshots.Room.RoomInfoSnapshot>> FetchRoomsAsync(
        string searchCode,
        NavigatorSearchFilterType filterType,
        string filterValue,
        PlayerId playerId,
        CancellationToken ct
    )
    {
        // Explicit filter overrides searchCode routing
        if (!string.IsNullOrWhiteSpace(filterValue))
        {
            return filterType switch
            {
                NavigatorSearchFilterType.RoomName => await _navigatorProvider
                    .GetRoomsByNameAsync(filterValue, ct)
                    .ConfigureAwait(false),
                NavigatorSearchFilterType.Owner => await _navigatorProvider
                    .GetRoomsByOwnerNameAsync(filterValue, ct)
                    .ConfigureAwait(false),
                NavigatorSearchFilterType.Tag => await _navigatorProvider
                    .GetRoomsByTagAsync(filterValue, ct)
                    .ConfigureAwait(false),
                _ => await _navigatorProvider
                    .GetRoomsByNameAsync(filterValue, ct)
                    .ConfigureAwait(false),
            };
        }

        NavigatorQueryType queryType = _navigatorProvider.ResolveQueryType(searchCode);

        return queryType switch
        {
            NavigatorQueryType.MyRooms => await _navigatorProvider
                .GetRoomsByOwnerAsync(playerId, ct)
                .ConfigureAwait(false),
            NavigatorQueryType.MyFavorites => await _navigatorProvider
                .GetFavoriteRoomsAsync(playerId, ct)
                .ConfigureAwait(false),
            NavigatorQueryType.ByFlatCategory => await _navigatorProvider
                .GetRoomsByCategoryAsync(0, ct)
                .ConfigureAwait(false),
            _ => await _navigatorProvider.GetAllRoomsAsync(ct).ConfigureAwait(false),
        };
    }
}
