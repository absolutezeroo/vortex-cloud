using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Furniture.Snapshots;

namespace Vortex.Primitives.Furniture.Providers;

public interface IFurnitureDefinitionProvider
{
    public FurnitureDefinitionSnapshot? TryGetDefinition(int id);
    public Task ReloadAsync(CancellationToken ct);
}
