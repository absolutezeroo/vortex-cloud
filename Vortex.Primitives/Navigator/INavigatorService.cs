using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Navigator.Enums;
using Vortex.Primitives.Orleans.Snapshots.Navigator;
using Vortex.Primitives.Players;
using Vortex.Primitives.Snapshots.Navigator;

namespace Vortex.Primitives.Navigator;

public interface INavigatorService
{
    Task<ImmutableArray<NavigatorTopLevelContextSnapshot>> GetTopLevelContextAsync();

    ImmutableArray<NavigatorFlatCategorySnapshot> GetFlatCategories();

    Task<ImmutableArray<NavigatorSearchResultSnapshot>> GetSearchResultsAsync(
        string searchCode,
        NavigatorSearchFilterType filterType,
        string filterValue,
        PlayerId playerId,
        CancellationToken ct
    );

    Task<ImmutableArray<NavigatorSearchResultBlockSnapshot>> GetCategoryBlocksAsync(
        CancellationToken ct
    );

    /// <summary>The public/official rooms list, one entry per staff-picked room with its live
    /// population folded in.</summary>
    Task<ImmutableArray<OfficialRoomEntrySnapshot>> GetOfficialRoomEntriesAsync(
        CancellationToken ct
    );

    /// <summary>Live and maximum population per flat category, in the shape the client's
    /// categories-with-visitor-count packet expects.</summary>
    Task<CategoriesWithVisitorCountSnapshot> GetCategoryVisitorCountsAsync(CancellationToken ct);
}
