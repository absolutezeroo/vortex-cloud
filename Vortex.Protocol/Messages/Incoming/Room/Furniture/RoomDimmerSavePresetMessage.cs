using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Protocol.Messages.Incoming.Room.Furniture;

/// <summary>
/// Writing one of the moodlight's three presets, and optionally switching to it.
/// </summary>
/// <remarks>
/// The wire order is preset number, effect, colour, brightness, apply, an unused boolean, then the
/// object id — <c>RoomSession.sendRoomDimmerSavePresetMessage</c> builds it that way and hardcodes
/// the sixth value to false. The object id being last, after two booleans, is why a parser that
/// reads it in the obvious position gets a colour's worth of garbage instead.
/// </remarks>
public record RoomDimmerSavePresetMessage : IMessageEvent
{
    /// <summary>1, 2 or 3 — the client only ever draws three slots.</summary>
    public required int PresetNumber { get; init; }

    /// <summary>1 tints the whole room, 2 only the background.</summary>
    public required int EffectId { get; init; }

    /// <summary>Formatted <c>#RRGGBB</c> by the client, which is also how it parses it back.</summary>
    public required string ColorHex { get; init; }

    /// <summary>Alpha the client shows as a brightness slider, 0-255.</summary>
    public required int Brightness { get; init; }

    /// <summary>Whether to switch the dimmer to this preset as well as store it.</summary>
    public required bool Apply { get; init; }

    public required RoomObjectId ObjectId { get; init; }
}
