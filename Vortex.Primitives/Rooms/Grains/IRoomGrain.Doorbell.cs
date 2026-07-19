using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Players;

namespace Vortex.Primitives.Rooms.Grains;

public partial interface IRoomGrain
{
    /// <summary>Registers a ring against a locked door and notifies the ringer plus every
    /// present owner/rights-holder. Returns false if the player is already ringing (no-op).</summary>
    public Task<bool> RegisterDoorbellRingAsync(PlayerId ringerId, CancellationToken ct);

    /// <summary>Owner and rights-holders currently present in the room — the set entitled to
    /// answer a doorbell ring.</summary>
    public Task<ImmutableArray<PlayerId>> GetPresentRightsHoldersAsync();

    /// <summary>Removes a pending ring, if any. Returns whether one was actually pending.</summary>
    public Task<bool> TryRemoveDoorbellRingAsync(PlayerId ringerId, CancellationToken ct);
}
