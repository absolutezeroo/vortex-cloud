using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;

namespace Vortex.Primitives.Players.Grains;

/// <summary>
/// The whole avatars a player owns, and which one they are wearing.
/// </summary>
/// <remarks>
/// A clothing furni hands over pieces; this hands over a character. Wearing one replaces the player's
/// figure outright, so the look they arrived with is kept as a fallback — that is the only way back
/// out, and the client depends on it.
/// </remarks>
public interface IPlayerNftWardrobeGrain : IGrainWithIntegerKey
{
    /// <summary>The copies this account holds, as the editor's tab lists them.</summary>
    public Task<ImmutableArray<NftAvatarSnapshot>> GetWardrobeAsync(CancellationToken ct);

    /// <summary>
    /// What the player is wearing, or null when they are simply themselves.
    /// <para>
    /// Null is not the same as an empty answer here, and the difference is not cosmetic: the client
    /// decides "an avatar is worn" by testing its token against null, and a string read off a packet
    /// is never null. Answering "nothing" would convince it an avatar is on, and its editor then
    /// loads a fallback look instead of the player's own — which, with no fallback to load, means the
    /// editor stops showing them at all.
    /// </para>
    /// </summary>
    public Task<NftOutfitSnapshot?> GetWornAsync(CancellationToken ct);

    /// <summary>
    /// Puts on a copy the player owns, named by the id the client was sent: their figure becomes its
    /// figure, and the look they had is remembered. Returns null when the copy is not theirs, or its
    /// model is no longer offered.
    /// </summary>
    public Task<NftOutfitSnapshot?> WearAsync(int copyId, CancellationToken ct);

    /// <summary>
    /// Takes the costume off, keeping whatever figure the player has just chosen. Called when they
    /// save an ordinary look, which is how the client expects to leave the tab.
    /// </summary>
    public Task RemoveWornAsync(CancellationToken ct);
}

/// <summary>One copy of an avatar, as the client reads it.</summary>
[GenerateSerializer, Immutable]
public sealed record NftAvatarSnapshot
{
    /// <summary>The copy's id. Sent as the tile's identity, echoed back to say which one to wear —
    /// and shown to the player, after a "#", as the number of their avatar.</summary>
    [Id(0)]
    public required int CopyId { get; init; }

    [Id(1)]
    public required string Figure { get; init; }

    [Id(2)]
    public required string Gender { get; init; }

    /// <summary>Unique to this copy. The client matches the worn outfit to a tile by this and
    /// nothing else, so the same copy must produce the same token everywhere.</summary>
    [Id(3)]
    public required string TokenId { get; init; }

    [Id(4)]
    public required string ContractKey { get; init; }
}

/// <summary>The avatar being worn, and the look to give back.</summary>
[GenerateSerializer, Immutable]
public sealed record NftOutfitSnapshot
{
    [Id(0)]
    public required string TokenId { get; init; }

    [Id(1)]
    public required string FallbackFigure { get; init; }

    [Id(2)]
    public required string FallbackGender { get; init; }
}
