using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Primitives.Players.Snapshots;

namespace Turbo.Primitives.Players.Grains;

public interface IPlayerBadgeGrain : IGrainWithIntegerKey
{
    public Task<ImmutableArray<PlayerBadgeSnapshot>> GetBadgesAsync(CancellationToken ct);
    public Task SetActivatedBadgesAsync(
        List<(int SlotId, string BadgeCode)> slots,
        CancellationToken ct
    );
}
