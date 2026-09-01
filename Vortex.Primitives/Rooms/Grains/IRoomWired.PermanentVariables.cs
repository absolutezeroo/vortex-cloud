using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;

namespace Vortex.Primitives.Rooms.Grains;

public partial interface IRoomWired
{
    public Task<WiredPermanentVariablesSnapshot> GetPermanentVariablesForEntityAsync(
        WiredVariableTargetType targetType,
        int targetId,
        CancellationToken ct
    );

    /// <summary>Writes or deletes one permanent wired variable on behalf of <paramref name="ctx"/>.
    /// </summary>
    /// <remarks>
    /// The actor is a parameter and not the caller's word for it: this is a grain method, so it is
    /// public to the whole cluster and the wired menu's own precondition -- rights in the room it
    /// was opened from -- has to be re-established here rather than in the packet handler.
    /// </remarks>
    public Task<bool> SetPermanentVariableAsync(
        ActionContext ctx,
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
