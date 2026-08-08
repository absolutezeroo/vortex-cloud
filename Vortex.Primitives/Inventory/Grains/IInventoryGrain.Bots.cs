using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Bots;

namespace Vortex.Primitives.Inventory.Grains;

public partial interface IInventoryGrain
{
    /// <summary>Bots the player owns and has not placed in a room.</summary>
    public Task<ImmutableArray<BotSnapshot>> GetAllBotSnapshotsAsync(CancellationToken ct);
}
