using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Navigator.Enums;
using Vortex.Primitives.Orleans.Snapshots.Navigator;
using Vortex.Primitives.Players;

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
}
