using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredtrading;

/// <summary>
/// Taking items back out of a chest, by kind rather than by id.
/// </summary>
/// <remarks>
/// The chest screen groups identical furni into one row with a count, so the client asks for "three
/// of this kind" and never names the individual items. Which three come out is the server's choice.
/// </remarks>
public record WithdrawWiredChestItemsMessage : IMessageEvent
{
    public required int ChestId { get; init; }

    /// <summary>Wall or floor. The client sends this as the first field of its ChestItemType.</summary>
    public required bool IsWallItem { get; init; }

    /// <summary>The sprite id every item of this kind shares.</summary>
    public required int TypeId { get; init; }

    /// <summary>Only ever set for posters, where the kind is the poster number rather than the
    /// sprite; empty for everything else.</summary>
    public required string LegacyPosterId { get; init; }

    /// <summary>How many of that kind to take out.</summary>
    public required int Count { get; init; }
}
