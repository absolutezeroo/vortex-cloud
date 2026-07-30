using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Inventory.Badges;

[GenerateSerializer, Immutable]
public sealed record BadgeReceivedEventMessageComposer : IComposer
{
    /// <summary>Named for what the client reads it as. The emulator has no numeric badge id, so
    /// this stays 0; the client only uses it to key its own badge model.</summary>
    [Id(0)]
    public int BadgeId { get; init; }

    [Id(1)]
    public required string BadgeCode { get; init; }

    /// <summary>How many players own the badge, and its rarity tier. The emulator tracks neither,
    /// but the client reads both unconditionally -- it used to read them off the end of the packet.</summary>
    [Id(2)]
    public int OwnerCount { get; init; }

    [Id(3)]
    public int BadgeRarityId { get; init; }
}
