using System.Threading;
using System.Threading.Tasks;
using Turbo.Primitives.Furniture.Admin;

namespace Turbo.Primitives.Furniture;

/// <summary>
/// CRUD for furniture_definitions, used by the dashboard's furniture admin surface. Every write
/// reloads <see cref="Providers.IFurnitureDefinitionProvider"/> so the live in-memory snapshot
/// handlers/serializers read from never drifts from the database.
/// </summary>
public interface IFurnitureAdminService
{
    Task<FurnitureAdminResult> CreateAsync(
        FurnitureDefinitionUpsertSpec spec,
        CancellationToken ct
    );
    Task<FurnitureAdminResult> UpdateAsync(
        int id,
        FurnitureDefinitionUpsertSpec spec,
        CancellationToken ct
    );
    Task<FurnitureAdminResult> DeleteAsync(int id, CancellationToken ct);
}
