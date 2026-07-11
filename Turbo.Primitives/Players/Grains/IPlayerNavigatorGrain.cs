using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Primitives.Orleans.Snapshots.Navigator;

namespace Turbo.Primitives.Players.Grains;

public interface IPlayerNavigatorGrain : IGrainWithIntegerKey
{
    Task<NavigatorWindowPreferencesSnapshot> GetWindowPreferencesAsync(CancellationToken ct);
    Task UpdateWindowPreferencesAsync(
        int x,
        int y,
        int width,
        int height,
        bool leftPaneHidden,
        int resultsMode,
        CancellationToken ct
    );

    Task<List<NavigatorQuickLinkSnapshot>> GetSavedSearchesAsync(CancellationToken ct);
    Task AddSavedSearchAsync(string searchCode, string filter, CancellationToken ct);
    Task DeleteSavedSearchAsync(int searchId, CancellationToken ct);

    Task<List<string>> GetCollapsedCategoriesAsync(CancellationToken ct);
    Task AddCollapsedCategoryAsync(string categoryName, CancellationToken ct);
    Task RemoveCollapsedCategoryAsync(string categoryName, CancellationToken ct);

    Task<int> GetViewModeAsync(string searchCode, CancellationToken ct);
    Task SetViewModeAsync(string searchCode, int viewMode, CancellationToken ct);

    Task AddFavouriteRoomAsync(int roomId, CancellationToken ct);
    Task RemoveFavouriteRoomAsync(int roomId, CancellationToken ct);

    Task<int> GetHomeRoomIdAsync(CancellationToken ct);
    Task SetHomeRoomIdAsync(int roomId, CancellationToken ct);
}
