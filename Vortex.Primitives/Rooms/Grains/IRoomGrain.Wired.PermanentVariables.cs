using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;

namespace Vortex.Primitives.Rooms.Grains;

public partial interface IRoomGrain
{
    public Task<WiredPermanentVariablesSnapshot> GetPermanentVariablesForEntityAsync(
        WiredVariableTargetType targetType,
        int targetId,
        CancellationToken ct
    );

    public Task<bool> SetPermanentVariableAsync(
        WiredVariableTargetType targetType,
        int targetId,
        string variableId,
        int value,
        int action,
        CancellationToken ct
    );

    public Task<WiredVariableOwnersPageSnapshot> GetVariableOwnersPageAsync(
        string variableId,
        int page,
        int pageSize,
        int userTypeFilter,
        int sortTypeFilter,
        CancellationToken ct
    );
}
