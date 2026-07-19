using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Pets;
using Vortex.Primitives.Pets.Snapshots;

namespace Vortex.Primitives.Inventory.Grains;

public partial interface IInventoryGrain
{
    public Task<PetSnapshot> CreatePetAsync(PetCreateRequest request, CancellationToken ct);
    public Task<PetSnapshot?> GetPetSnapshotAsync(int petId, CancellationToken ct);
    public Task<ImmutableArray<PetSnapshot>> GetAllPetSnapshotsAsync(CancellationToken ct);
}
