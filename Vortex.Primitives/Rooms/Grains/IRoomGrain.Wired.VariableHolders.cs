using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;

namespace Vortex.Primitives.Rooms.Grains;

public partial interface IRoomGrain
{
    public Task<(
        WiredVariableSnapshot Variable,
        List<(int ObjectId, int Value)> Holders
    )?> GetVariableHoldersByNameAsync(string variableName, CancellationToken ct);
}
