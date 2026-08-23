using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;

/// <summary>
/// What is on a wired trade's table.
/// </summary>
/// <remarks>
/// The first stretch is the ordinary two-sided trade item list, byte for byte — the client parses
/// it with the very same parser class the player-to-player trade uses, then reads the two fields
/// below. First side is the player, second is the room.
/// <para>
/// <see cref="CanAccept"/> is the server's decision, not the client's: the accept button is live
/// only while this says so.
/// </para>
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record WiredTradeItemsUpdateMessageComposer : IComposer
{
    [Id(0)]
    public required int FirstUserId { get; init; }

    [Id(1)]
    public required ImmutableArray<FurnitureItemSnapshot> FirstUserItems { get; init; }

    [Id(2)]
    public required int FirstUserCredits { get; init; }

    [Id(3)]
    public required int SecondUserId { get; init; }

    [Id(4)]
    public required ImmutableArray<FurnitureItemSnapshot> SecondUserItems { get; init; }

    [Id(5)]
    public required int SecondUserCredits { get; init; }

    [Id(6)]
    public required bool CanAccept { get; init; }

    /// <summary>AS3's own name, and it says nothing: the model stores it and never reads it.</summary>
    [Id(7)]
    public required int Extra { get; init; }
}
