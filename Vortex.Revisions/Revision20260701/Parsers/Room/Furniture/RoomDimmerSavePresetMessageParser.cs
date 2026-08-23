using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Object;
using Vortex.Protocol.Messages.Incoming.Room.Furniture;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Furniture;

internal class RoomDimmerSavePresetMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        int presetNumber = packet.PopInt();
        int effectId = packet.PopInt();
        string colorHex = packet.PopString();
        int brightness = packet.PopInt();
        bool apply = packet.PopBoolean();

        // Hardcoded false by the client and read by nothing. Popped rather than skipped so the
        // object id after it lands on the right offset.
        _ = packet.PopBoolean();

        return new RoomDimmerSavePresetMessage
        {
            PresetNumber = presetNumber,
            EffectId = effectId,
            ColorHex = colorHex,
            Brightness = brightness,
            Apply = apply,
            ObjectId = new RoomObjectId(packet.PopInt()),
        };
    }
}
