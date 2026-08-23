using System.Collections.Immutable;
using Orleans;

namespace Vortex.Primitives.Rooms.Snapshots.Wired;

// These are a contract's terms, not a message: no IComposer, no IMessageEvent, and the room engine
// reads and writes them as domain values. They lived in the outgoing-message tree only because that
// is where the client first needed them, which put a wire namespace inside grain contracts and kept
// the hub from being protocol-free. The composers and parsers that carry them import them from here
// now -- protocol depending on contracts is the allowed direction.

/// <summary>One term of a contract: so many coins, or so many of one kind of furniture.</summary>
/// <remarks>
/// <see cref="ItemType"/> is read only when <see cref="IsFurni"/> — the wire carries it for a furni
/// term and nothing for a coin one, which is why it is nullable here rather than always present.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record TradeContractNode
{
    /// <summary>Coin terms are 0 on the wire, furni terms 1.</summary>
    [Id(0)]
    public required bool IsFurni { get; init; }

    [Id(1)]
    public required int Amount { get; init; }

    [Id(2)]
    public TradeContractItemType? ItemType { get; init; }
}

/// <summary>Which furniture a term asks for: a sprite, a side, and a poster number.</summary>
[GenerateSerializer, Immutable]
public sealed record TradeContractItemType
{
    [Id(0)]
    public required bool IsWallItem { get; init; }

    [Id(1)]
    public required int SpriteId { get; init; }

    /// <summary>Empty for anything that is not a poster; the client reads empty as "none".</summary>
    [Id(2)]
    public required string LegacyPosterId { get; init; }
}

/// <summary>
/// One alternative of a contract's terms — its nodes are read as "and", the rules around it as "or".
/// </summary>
[GenerateSerializer, Immutable]
public sealed record TradeContractRule
{
    [Id(0)]
    public required ImmutableArray<TradeContractNode> Nodes { get; init; }
}

/// <summary>
/// What a custom contract asks for and hands back, in the shape the client reads it.
/// </summary>
/// <remarks>
/// Both sides are optional and each is announced by a flag before it: a payment-only contract sends
/// its give rules and no receive rule at all. <see cref="Mode"/> decides what follows it — 1 is
/// followed by a fixed multiplier, 2 by an auto-multiplier ceiling, 0 by neither — and the client
/// defaults both to 1 rather than 0, so a contract naming neither still trades once.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record TradeContract
{
    /// <summary>The alternatives the player may pay with. Null sends the "no give side" flag.</summary>
    [Id(0)]
    public ImmutableArray<TradeContractRule>? YouGiveRules { get; init; }

    /// <summary>What comes back. Null is a payment-only contract.</summary>
    [Id(1)]
    public TradeContractRule? YouGetRule { get; init; }

    /// <summary>0 takes the contract once, 1 a fixed number of times, 2 up to a ceiling.</summary>
    [Id(2)]
    public required int Mode { get; init; }

    /// <summary>Written only under mode 1.</summary>
    [Id(3)]
    public int Multiplier { get; init; } = 1;

    /// <summary>Written only under mode 2.</summary>
    [Id(4)]
    public int AutoMultiplierMax { get; init; } = 1;
}
