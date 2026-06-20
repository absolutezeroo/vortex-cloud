using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Turbo.Database.Context;
using Turbo.Database.Entities.Navigator;
using Turbo.Database.Entities.Room;
using Turbo.Primitives.Navigator;
using Turbo.Primitives.Navigator.Enums;
using Turbo.Primitives.Orleans.Snapshots.Navigator;
using Turbo.Primitives.Orleans.Snapshots.Room;
using Turbo.Primitives.Players;

namespace Turbo.Navigator;

public sealed class NavigatorProvider(
    IDbContextFactory<TurboDbContext> dbCtxFactory,
    ILogger<NavigatorProvider> logger
) : INavigatorProvider
{
    private readonly IDbContextFactory<TurboDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly ILogger<NavigatorProvider> _logger = logger;

    private ImmutableArray<NavigatorTopLevelContextSnapshot> _topLevelContexts = [];
    private ImmutableArray<NavigatorFlatCategorySnapshot> _flatCategories = [];
    private ImmutableDictionary<string, NavigatorQueryType> _queryTypeBySearchCode =
        ImmutableDictionary<string, NavigatorQueryType>.Empty;

    public Task<ImmutableArray<NavigatorTopLevelContextSnapshot>> GetTopLevelContextsAsync() =>
        Task.FromResult(_topLevelContexts);

    public NavigatorQueryType ResolveQueryType(string searchCode) =>
        _queryTypeBySearchCode.TryGetValue(searchCode, out NavigatorQueryType qt)
            ? qt
            : NavigatorQueryType.AllRooms;

    public ImmutableArray<NavigatorFlatCategorySnapshot> GetFlatCategories() => _flatCategories;

    public async Task<List<RoomInfoSnapshot>> GetAllRoomsAsync(CancellationToken ct = default)
    {
        await using TurboDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        return await BuildRoomQuery(dbCtx)
            .OrderByDescending(x => x.UsersNow)
            .ToRoomInfoSnapshots(ct)
            .ConfigureAwait(false);
    }

    public async Task<List<RoomInfoSnapshot>> GetRoomsByOwnerAsync(
        PlayerId playerId,
        CancellationToken ct = default
    )
    {
        await using TurboDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        return await BuildRoomQuery(dbCtx)
            .Where(x => x.PlayerEntityId == playerId.Value)
            .ToRoomInfoSnapshots(ct)
            .ConfigureAwait(false);
    }

    public async Task<List<RoomInfoSnapshot>> GetRoomsByCategoryAsync(
        int categoryId,
        CancellationToken ct = default
    )
    {
        await using TurboDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        return await BuildRoomQuery(dbCtx)
            .Where(x => x.NavigatorCategoryEntityId == categoryId)
            .ToRoomInfoSnapshots(ct)
            .ConfigureAwait(false);
    }

    public async Task<List<RoomInfoSnapshot>> GetRoomsByNameAsync(
        string name,
        CancellationToken ct = default
    )
    {
        await using TurboDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        string lower = name.ToLowerInvariant();

        return await BuildRoomQuery(dbCtx)
            .Where(x => x.Name.ToLower().Contains(lower))
            .ToRoomInfoSnapshots(ct)
            .ConfigureAwait(false);
    }

    public async Task<List<RoomInfoSnapshot>> GetRoomsByOwnerNameAsync(
        string ownerName,
        CancellationToken ct = default
    )
    {
        await using TurboDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        string lower = ownerName.ToLowerInvariant();

        return await BuildRoomQuery(dbCtx)
            .Where(x => x.PlayerEntity.Name.ToLower().Contains(lower))
            .ToRoomInfoSnapshots(ct)
            .ConfigureAwait(false);
    }

    public async Task<List<RoomInfoSnapshot>> GetRoomsByTagAsync(
        string tag,
        CancellationToken ct = default
    )
    {
        // Tags not stored in DB yet
        await Task.CompletedTask.ConfigureAwait(false);
        return [];
    }

    public async Task<List<RoomInfoSnapshot>> GetFavoriteRoomsAsync(
        PlayerId playerId,
        CancellationToken ct = default
    )
    {
        // Favorites table not implemented yet
        await Task.CompletedTask.ConfigureAwait(false);
        return [];
    }

    public async Task ReloadAsync(CancellationToken ct = default)
    {
        await using TurboDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        List<NavigatorTopLevelContextEntity> topLevelEntities = await dbCtx
            .NavigatorTopLevelContexts.AsNoTracking()
            .Include(x => x.QuickLinks)
            .Where(x => x.Visible && x.DeletedAt == null)
            .OrderBy(x => x.OrderNum)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        _topLevelContexts =
        [
            .. topLevelEntities.Select(x => new NavigatorTopLevelContextSnapshot
            {
                SearchCode = x.SearchCode,
                QueryType = x.QueryType,
                QuickLinks =
                [
                    .. (x.QuickLinks ?? [])
                        .OrderBy(q => q.OrderNum)
                        .Select(q => new NavigatorQuickLinkSnapshot
                        {
                            Id = q.Id,
                            SearchCode = q.SearchCode,
                            Filter = q.Filter,
                            Localization = q.Localization,
                            QueryType = q.QueryType,
                        }),
                ],
            }),
        ];

        _queryTypeBySearchCode = _topLevelContexts
            .SelectMany(ctx =>
                ctx.QuickLinks.Select(ql => (ql.SearchCode, ql.QueryType))
                    .Append((ctx.SearchCode, ctx.QueryType))
            )
            .ToImmutableDictionary(x => x.SearchCode, x => x.QueryType);

        List<NavigatorFlatCategoryEntity> flatCatEntities = await dbCtx
            .NavigatorFlatCategories.AsNoTracking()
            .Where(x => x.Visible && x.DeletedAt == null)
            .OrderBy(x => x.OrderNum)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        _flatCategories =
        [
            .. flatCatEntities.Select(x => new NavigatorFlatCategorySnapshot
            {
                Id = x.Id,
                Name = x.Name,
                MinRank = x.MinRank,
                Visible = x.Visible,
                Automatic = x.Automatic,
                AutomaticCategory = x.AutomaticCategory ?? string.Empty,
                GlobalCategory = x.GlobalCategory ?? string.Empty,
                StaffOnly = x.StaffOnly,
            }),
        ];

        _logger.LogInformation(
            "Loaded navigator snapshot: TotalTopLevelContexts={Count}, FlatCategories={FlatCount}",
            _topLevelContexts.Length,
            _flatCategories.Length
        );
    }

    private static IQueryable<Database.Entities.Room.RoomEntity> BuildRoomQuery(
        TurboDbContext dbCtx
    ) => dbCtx.Rooms.AsNoTracking().Where(x => x.DeletedAt == null).Include(x => x.PlayerEntity);
}

file static class RoomQueryExtensions
{
    public static async Task<List<RoomInfoSnapshot>> ToRoomInfoSnapshots(
        this IQueryable<Database.Entities.Room.RoomEntity> query,
        CancellationToken ct
    )
    {
        List<RoomEntity> entities = await query.ToListAsync(ct).ConfigureAwait(false);

        return
        [
            .. entities.Select(x => new RoomInfoSnapshot
            {
                RoomId = x.Id,
                Name = x.Name ?? string.Empty,
                Description = x.Description ?? string.Empty,
                OwnerId = (PlayerId)x.PlayerEntityId,
                OwnerName = x.PlayerEntity?.Name ?? string.Empty,
                Population = x.UsersNow,
                DoorMode = x.DoorMode,
                PlayersMax = x.PlayersMax,
                TradeType = x.TradeType,
                Score = 0,
                Ranking = 0,
                CategoryId = x.NavigatorCategoryEntityId ?? -1,
                Tags = [],
                AllowBlocking = x.AllowBlocking,
                AllowPets = x.AllowPets,
                AllowPetsEat = x.AllowPetsEat,
                LastUpdatedUtc = DateTime.UtcNow,
            }),
        ];
    }
}
