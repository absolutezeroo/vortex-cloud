using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Turbo.Primitives.Navigator.Enums;
using Turbo.Primitives.Orleans.Snapshots.Navigator;
using Turbo.Primitives.Players;

namespace Turbo.Primitives.Navigator;

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
