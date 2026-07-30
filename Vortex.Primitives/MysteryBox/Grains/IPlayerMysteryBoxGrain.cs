using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.MysteryBox.Snapshots;

namespace Vortex.Primitives.MysteryBox.Grains;

/// <summary>
/// Per-player gateway to the two mystery box halves. The box half is furniture (ownership already
/// lives in the <c>furniture</c> table, resolved here against the cached box definitions); the key
/// half is a server-side token in <c>player_mystery_box_keys</c>. Owns key grant/consumption and
/// pushes the toolbar tracker.
/// </summary>
public interface IPlayerMysteryBoxGrain : IGrainWithIntegerKey
{
    /// <summary>The colours the toolbar tracker should show for this player.</summary>
    public Task<MysteryBoxTrackerSnapshot> GetTrackerAsync(CancellationToken ct);

    /// <summary>Recomputes the tracker and pushes it to the player (no-op when offline).</summary>
    public Task PushTrackerAsync(CancellationToken ct);

    /// <summary>True when the player holds at least one unspent key of <paramref name="color"/>.</summary>
    public Task<bool> HasKeyAsync(string color, CancellationToken ct);

    /// <summary>
    /// Hands the player a key. <paramref name="source"/> is recorded for auditing (e.g.
    /// "quest:xmas12", "staff:admin"). Returns false when the colour is not one the client can
    /// render. Pushes the refreshed tracker.
    /// </summary>
    public Task<bool> GrantKeyAsync(string color, string source, CancellationToken ct);

    /// <summary>
    /// Spends one key of <paramref name="color"/>, oldest first. Returns false when the player holds
    /// none -- callers must treat that as "the open does not happen" rather than granting the prize
    /// anyway.
    /// </summary>
    public Task<bool> TryConsumeKeyAsync(string color, CancellationToken ct);

    /// <summary>
    /// Grants <paramref name="prize"/> to the player and returns what the reward window should draw,
    /// or null when the prize could not be granted (unknown definition, malformed parameters).
    /// </summary>
    public Task<MysteryBoxPrizeAward?> GrantPrizeAsync(
        MysteryBoxPrizeSnapshot prize,
        CancellationToken ct
    );
}
