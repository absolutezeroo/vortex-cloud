using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// The chests as a wired box reaches them: it answers a fixed count and records what it was asked,
/// so a test can pin both the comparison and the slots the box read its chests out of.
/// </summary>
internal sealed class FakeChestAccess(int held = 0) : IRoomChestAccess
{
    public int Held { get; set; } = held;

    public List<(IReadOnlyList<int> ChestIds, IReadOnlyList<int> KindExampleIds)> Counted { get; } =
    [];

    public Task<int> CountChestItemsAsync(
        IReadOnlyList<int> chestIds,
        IReadOnlyList<int> kindExampleItemIds,
        CancellationToken ct
    )
    {
        Counted.Add((chestIds, kindExampleItemIds));

        return Task.FromResult(Held);
    }

    public Task<int> PayOutChestCreditsAsync(
        int chestId,
        PlayerId playerId,
        int amount,
        bool everything,
        CancellationToken ct
    ) => Task.FromResult(0);

    public Task<int> PayOutChestItemsAsync(
        int chestId,
        PlayerId playerId,
        int count,
        CancellationToken ct
    ) => Task.FromResult(0);
}
