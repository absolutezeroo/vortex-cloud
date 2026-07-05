using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Orleans;
using Turbo.Database.Context;
using Turbo.Database.Entities.Players;
using Turbo.Primitives.Orleans.Snapshots.Navigator;
using Turbo.Primitives.Players.Grains;

namespace Turbo.Players.Grains;

internal sealed class PlayerNavigatorGrain(IDbContextFactory<TurboDbContext> dbCtxFactory)
    : Grain,
        IPlayerNavigatorGrain
{
    private readonly IDbContextFactory<TurboDbContext> _dbCtxFactory = dbCtxFactory;

    private NavigatorWindowPreferencesSnapshot _preferences = new();
    private List<NavigatorQuickLinkSnapshot> _savedSearches = [];
    private List<string> _collapsedCategories = [];
    private Dictionary<string, int> _viewModes = [];

    public override async Task OnActivateAsync(CancellationToken ct)
    {
        await HydrateAsync(ct);
    }

    public Task<NavigatorWindowPreferencesSnapshot> GetWindowPreferencesAsync(
        CancellationToken ct
    ) => Task.FromResult(_preferences);

    public async Task UpdateWindowPreferencesAsync(
        int x,
        int y,
        int width,
        int height,
        bool leftPaneHidden,
        int resultsMode,
        CancellationToken ct
    )
    {
        _preferences = new NavigatorWindowPreferencesSnapshot
        {
            WindowX = x,
            WindowY = y,
            WindowWidth = width,
            WindowHeight = height,
            LeftPaneHidden = leftPaneHidden,
            ResultsMode = resultsMode,
        };

        await using TurboDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        int playerId = (int)this.GetPrimaryKeyLong();

        PlayerNavigatorPreferencesEntity? existing = await dbCtx
            .PlayerNavigatorPreferences.FirstOrDefaultAsync(e => e.PlayerEntityId == playerId, ct)
            .ConfigureAwait(true);

        if (existing is null)
        {
            dbCtx.PlayerNavigatorPreferences.Add(
                new PlayerNavigatorPreferencesEntity
                {
                    PlayerEntityId = playerId,
                    WindowX = x,
                    WindowY = y,
                    WindowWidth = width,
                    WindowHeight = height,
                    LeftPaneHidden = leftPaneHidden,
                    ResultsMode = resultsMode,
                }
            );
        }
        else
        {
            existing.WindowX = x;
            existing.WindowY = y;
            existing.WindowWidth = width;
            existing.WindowHeight = height;
            existing.LeftPaneHidden = leftPaneHidden;
            existing.ResultsMode = resultsMode;
        }

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);
    }

    public Task<List<NavigatorQuickLinkSnapshot>> GetSavedSearchesAsync(CancellationToken ct) =>
        Task.FromResult(_savedSearches);

    public async Task AddSavedSearchAsync(string searchCode, string filter, CancellationToken ct)
    {
        await using TurboDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        int playerId = (int)this.GetPrimaryKeyLong();
        int orderNum = _savedSearches.Count;

        PlayerNavigatorSavedSearchEntity entity = new PlayerNavigatorSavedSearchEntity
        {
            PlayerEntityId = playerId,
            SearchCode = searchCode,
            Filter = filter,
            OrderNum = orderNum,
        };

        dbCtx.PlayerNavigatorSavedSearches.Add(entity);
        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

        _savedSearches.Add(
            new NavigatorQuickLinkSnapshot
            {
                Id = entity.Id,
                SearchCode = searchCode,
                Filter = filter,
                Localization = string.Empty,
            }
        );
    }

    public async Task DeleteSavedSearchAsync(int searchId, CancellationToken ct)
    {
        await using TurboDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        int playerId = (int)this.GetPrimaryKeyLong();

        await dbCtx
            .PlayerNavigatorSavedSearches.Where(e =>
                e.Id == searchId && e.PlayerEntityId == playerId
            )
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(true);

        _savedSearches.RemoveAll(s => s.Id == searchId);
    }

    public Task<List<string>> GetCollapsedCategoriesAsync(CancellationToken ct) =>
        Task.FromResult(_collapsedCategories);

    public async Task AddCollapsedCategoryAsync(string categoryName, CancellationToken ct)
    {
        if (_collapsedCategories.Contains(categoryName))
        {
            return;
        }

        await using TurboDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        dbCtx.PlayerNavigatorCollapsedCategories.Add(
            new PlayerNavigatorCollapsedCategoryEntity
            {
                PlayerEntityId = (int)this.GetPrimaryKeyLong(),
                CategoryName = categoryName,
            }
        );

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

        _collapsedCategories.Add(categoryName);
    }

    public async Task RemoveCollapsedCategoryAsync(string categoryName, CancellationToken ct)
    {
        if (!_collapsedCategories.Contains(categoryName))
        {
            return;
        }

        await using TurboDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        int playerId = (int)this.GetPrimaryKeyLong();

        await dbCtx
            .PlayerNavigatorCollapsedCategories.Where(e =>
                e.PlayerEntityId == playerId && e.CategoryName == categoryName
            )
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(true);

        _collapsedCategories.Remove(categoryName);
    }

    public Task<int> GetViewModeAsync(string searchCode, CancellationToken ct) =>
        Task.FromResult(_viewModes.TryGetValue(searchCode, out int mode) ? mode : 0);

    public async Task SetViewModeAsync(string searchCode, int viewMode, CancellationToken ct)
    {
        _viewModes[searchCode] = viewMode;

        await using TurboDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        int playerId = (int)this.GetPrimaryKeyLong();

        PlayerNavigatorViewModeEntity? existing = await dbCtx
            .PlayerNavigatorViewModes.FirstOrDefaultAsync(
                e => e.PlayerEntityId == playerId && e.SearchCode == searchCode,
                ct
            )
            .ConfigureAwait(true);

        if (existing is null)
        {
            dbCtx.PlayerNavigatorViewModes.Add(
                new PlayerNavigatorViewModeEntity
                {
                    PlayerEntityId = playerId,
                    SearchCode = searchCode,
                    ViewMode = viewMode,
                }
            );
        }
        else
        {
            existing.ViewMode = viewMode;
        }

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);
    }

    public async Task AddFavouriteRoomAsync(int roomId, CancellationToken ct)
    {
        await using TurboDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        int playerId = (int)this.GetPrimaryKeyLong();

        bool alreadyFavourited = await dbCtx
            .PlayerFavouriteRooms.AnyAsync(
                f => f.PlayerEntityId == playerId && f.RoomEntityId == roomId,
                ct
            )
            .ConfigureAwait(true);

        if (alreadyFavourited)
        {
            return;
        }

        dbCtx.PlayerFavouriteRooms.Add(
            new PlayerFavoriteRoomsEntity
            {
                PlayerEntityId = playerId,
                RoomEntityId = roomId,
                PlayerEntity = null!,
                RoomEntity = null!,
            }
        );

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);
    }

    public async Task RemoveFavouriteRoomAsync(int roomId, CancellationToken ct)
    {
        await using TurboDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        int playerId = (int)this.GetPrimaryKeyLong();

        await dbCtx
            .PlayerFavouriteRooms.Where(f =>
                f.PlayerEntityId == playerId && f.RoomEntityId == roomId
            )
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(true);
    }

    private async Task HydrateAsync(CancellationToken ct)
    {
        await using TurboDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        int playerId = (int)this.GetPrimaryKeyLong();

        PlayerNavigatorPreferencesEntity? prefsEntity = await dbCtx
            .PlayerNavigatorPreferences.AsNoTracking()
            .FirstOrDefaultAsync(e => e.PlayerEntityId == playerId, ct)
            .ConfigureAwait(true);

        _preferences = prefsEntity is null
            ? new NavigatorWindowPreferencesSnapshot()
            : new NavigatorWindowPreferencesSnapshot
            {
                WindowX = prefsEntity.WindowX,
                WindowY = prefsEntity.WindowY,
                WindowWidth = prefsEntity.WindowWidth,
                WindowHeight = prefsEntity.WindowHeight,
                LeftPaneHidden = prefsEntity.LeftPaneHidden,
                ResultsMode = prefsEntity.ResultsMode,
            };

        List<PlayerNavigatorSavedSearchEntity> savedSearchEntities = await dbCtx
            .PlayerNavigatorSavedSearches.AsNoTracking()
            .Where(e => e.PlayerEntityId == playerId)
            .OrderBy(e => e.OrderNum)
            .ToListAsync(ct)
            .ConfigureAwait(true);

        _savedSearches =
        [
            .. savedSearchEntities.Select(e => new NavigatorQuickLinkSnapshot
            {
                Id = e.Id,
                SearchCode = e.SearchCode,
                Filter = e.Filter,
                Localization = string.Empty,
            }),
        ];

        List<PlayerNavigatorCollapsedCategoryEntity> collapsedEntities = await dbCtx
            .PlayerNavigatorCollapsedCategories.AsNoTracking()
            .Where(e => e.PlayerEntityId == playerId)
            .ToListAsync(ct)
            .ConfigureAwait(true);

        _collapsedCategories = [.. collapsedEntities.Select(e => e.CategoryName)];

        List<PlayerNavigatorViewModeEntity> viewModeEntities = await dbCtx
            .PlayerNavigatorViewModes.AsNoTracking()
            .Where(e => e.PlayerEntityId == playerId)
            .ToListAsync(ct)
            .ConfigureAwait(true);

        _viewModes = viewModeEntities.ToDictionary(e => e.SearchCode, e => e.ViewMode);
    }
}
