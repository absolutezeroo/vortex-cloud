using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Turbo.Primitives.Rooms.Snapshots.Wired.Variables;

namespace Turbo.Primitives.Rooms.Grains;

public partial interface IRoomGrain
{
    public Task<(
        WiredVariableSnapshot Variable,
        List<(int ObjectId, int Value)> Holders
    )?> GetVariableHoldersByNameAsync(string variableName, CancellationToken ct);
}
