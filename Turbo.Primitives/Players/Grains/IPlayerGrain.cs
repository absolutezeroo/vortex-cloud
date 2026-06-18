using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Primitives.Orleans.Snapshots.Players;
using Turbo.Primitives.Players.Enums;
using Turbo.Primitives.Rooms.Enums;

namespace Turbo.Primitives.Grains.Players;

public interface IPlayerGrain : IGrainWithIntegerKey
{
    public Task SetFigureAsync(string figure, AvatarGenderType gender, CancellationToken ct);
    public Task SetMottoAsync(string text, CancellationToken ct);
    public Task<PlayerSummarySnapshot> GetSummaryAsync(CancellationToken ct);

    public Task<PlayerExtendedProfileSnapshot> GetExtendedProfileSnapshotAsync(
        CancellationToken ct
    );

    public Task<ClubSubscriptionSnapshot> GetClubSubscriptionAsync(CancellationToken ct);

    public Task<ClubPurchaseResult> PurchaseClubAsync(
        int months,
        bool isVip,
        int costCredits,
        CancellationToken ct
    );

    public Task<bool> TryConsumeClubGiftAsync(string productCode, CancellationToken ct);
    public Task TrackCreditSpendAsync(int credits, CancellationToken ct);
}
