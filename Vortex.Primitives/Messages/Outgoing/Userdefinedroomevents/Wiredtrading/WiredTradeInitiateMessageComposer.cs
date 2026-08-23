using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;

/// <summary>
/// Opens the inventory's wired-trade screen with the contract it has to satisfy.
/// </summary>
/// <remarks>
/// The requirement's <see cref="RequirementType"/> is what the client's
/// <c>WiredTradeRequirementsModel.canOfferFurni()</c> branches on: 0 accepts only credit furniture,
/// 1 refuses it, 2 accepts anything tradeable, and 4 defers to a rules block this composer does not
/// carry — a chest deposit has no "you get" side, so it never asks for one.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record WiredTradeInitiateMessageComposer : IComposer
{
    [Id(0)]
    public required int RequirementType { get; init; }

    /// <summary>Shown as what the player receives. Empty for a deposit: nothing comes back.</summary>
    [Id(1)]
    public required string YouGetText { get; init; }

    /// <summary>Which of the screen's layouts to wear. The client passes it through unread.</summary>
    [Id(2)]
    public required string LayoutType { get; init; }

    [Id(3)]
    public required bool ShowRequirementsImmediate { get; init; }

    /// <summary>Set when this replaces a trade already open, which the model closes first.</summary>
    [Id(4)]
    public required bool OverridePreviousTrade { get; init; }

    [Id(5)]
    public required int TimeoutSeconds { get; init; }

    /// <summary>
    /// The rules block, written only for a custom contract.
    /// </summary>
    /// <remarks>
    /// The client reads it if and only if <see cref="RequirementType"/> is 4, so the two have to
    /// agree: a 4 with nothing here desynchronises the rest of the message, and a contract under
    /// any other type is never read. Null for the three "any furni" types.
    /// </remarks>
    [Id(6)]
    public TradeContract? Contract { get; init; }
}
