using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;

namespace Vortex.Primitives.Fishing.Grains;

/// <summary>
/// One player's fishing progression: two levels, two XP counters, the token balance and the
/// Fishopedia rows behind them.
/// </summary>
/// <remarks>
/// <para>
/// Keyed by player because every write here is a read-modify-write of the same row, and catches
/// arrive on a timer that does not wait for the previous one to finish. A player-keyed grain settles
/// that without putting the hotel behind one lock.
/// </para>
/// <para>
/// Separate from <see cref="IFishingSessionGrain"/> on purpose: progression outlives a session and
/// is read by the book, the shop and the login push, none of which involve a spot.
/// </para>
/// </remarks>
public interface IFishingPlayerGrain : IGrainWithIntegerKey
{
    /// <summary>
    /// The player's standing. <paramref name="sessionCatchCount"/> comes from the session grain,
    /// which is the only thing that knows it; pass zero when no session is running.
    /// </summary>
    Task<FishingPlayerStateSnapshot> GetStateAsync(int sessionCatchCount, CancellationToken ct);

    /// <summary>Every species this player has caught. Absent species are undiscovered.</summary>
    Task<ImmutableArray<FishingRecordSnapshot>> GetRecordsAsync(CancellationToken ct);

    /// <summary>
    /// Applies one catch: banks the XP against both curves, adds the tokens the daily cap still
    /// allows, and writes the Fishopedia row. Answers what was actually granted.
    /// </summary>
    Task<FishingCatchOutcome> ApplyCatchAsync(FishingCatchProposal proposal, CancellationToken ct);

    /// <summary>
    /// Pushes the player's state and records to their session. Called after a catch and at login, so
    /// the client never has to ask for either.
    /// </summary>
    Task PushStateAsync(int sessionCatchCount, CancellationToken ct);

    /// <summary>
    /// The record row behind <paramref name="recordId"/>, or null when it is not this player's.
    /// </summary>
    Task<FishingRecordSnapshot?> FindRecordAsync(int recordId, CancellationToken ct);

    /// <summary>
    /// Turns one of the player's records into a mountable trophy in their inventory.
    /// </summary>
    /// <remarks>
    /// Here rather than in a handler because the record has to be the caller's own, and the check and
    /// the grant belong on the same side of the grain boundary — a record id is guessable, and the
    /// ownership test is the only thing standing between a client and somebody else's catch.
    ///
    /// <para>Answers false when the record is not theirs, or when the hotel has configured no trophy
    /// furniture. Both are refusals the client is told nothing about beyond the absence of an item;
    /// neither is worth an error code, because neither can happen to an honest client.</para>
    /// </remarks>
    Task<bool> MountRecordAsync(int recordId, CancellationToken ct);
}

/// <summary>
/// What the session grain proposes a catch is worth, before the player grain applies the caps.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record FishingCatchProposal
{
    [Id(0)]
    public required int SpeciesId { get; init; }

    [Id(1)]
    public required int Weight { get; init; }

    /// <summary>Already multiplied by the rod tier and by the frenzy, if one is running.</summary>
    [Id(2)]
    public required int Xp { get; init; }

    [Id(3)]
    public required int Currency { get; init; }

    /// <summary>A Golden Fish — only Hook Havoc and frenzies produce one.</summary>
    [Id(4)]
    public required bool Golden { get; init; }
}
