using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.RaidProtection;

namespace Vortex.Rooms.Grains;

public sealed partial class RoomGrain
{
    public Task<RoomRaidProtectionSnapshot?> GetRaidProtectionAsync(PlayerId viewerId) =>
        RaidProtectionSystem.GetSettingsAsync(viewerId);

    public Task<RaidProtectionSaveOutcome> SaveRaidProtectionAsync(
        ActionContext actorCtx,
        RoomRaidProtectionSnapshot draft,
        bool confirmed,
        CancellationToken ct
    ) => RaidProtectionSystem.SaveSettingsAsync(actorCtx, draft, confirmed, ct);

    public Task<RaidEntryDecision> EvaluateEntryAsync(PlayerId playerId, CancellationToken ct) =>
        RaidProtectionSystem.EvaluateEntryAsync(playerId, ct);
}
