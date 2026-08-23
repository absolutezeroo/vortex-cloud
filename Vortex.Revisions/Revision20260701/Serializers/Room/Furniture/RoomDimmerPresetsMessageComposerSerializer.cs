using Vortex.Protocol.Messages.Outgoing.Room.Furniture;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Snapshots.Furniture;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Furniture;

internal class RoomDimmerPresetsMessageComposerSerializer(int header)
    : AbstractSerializer<RoomDimmerPresetsMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        RoomDimmerPresetsMessageComposer message
    )
    {
        // The count comes first and the selected id second, both before the presets themselves —
        // the parser reads the loop bound, then the selection, then loops. Writing the selection
        // after the presets, which reads more naturally, shifts every field the client reads.
        packet.WriteInteger(message.Presets.Length).WriteInteger(message.SelectedPresetId);

        foreach (RoomDimmerPresetSnapshot preset in message.Presets)
        {
            packet
                .WriteInteger(preset.Id)
                .WriteInteger(preset.EffectId)
                .WriteString(preset.ColorHex)
                .WriteInteger(preset.Brightness);
        }

        packet.WriteBoolean(message.IsOn).WriteInteger(message.ItemId);
    }
}
