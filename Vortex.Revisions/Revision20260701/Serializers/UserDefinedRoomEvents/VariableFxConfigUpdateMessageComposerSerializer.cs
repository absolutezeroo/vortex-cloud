using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents;

/// <summary>
/// Field order is the client's parse order, read straight off its own parser: count, then per
/// config the thirteen scalars and the extra map.
/// </summary>
internal class VariableFxConfigUpdateMessageComposerSerializer(int header)
    : AbstractSerializer<VariableFxConfigUpdateMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        VariableFxConfigUpdateMessageComposer message
    )
    {
        packet.WriteInteger(message.Configs.Length);

        foreach (WiredVariableFxConfigSnapshot config in message.Configs)
        {
            packet
                .WriteInteger(config.ConfigId)
                .WriteBoolean(config.IsUserFx)
                .WriteInteger(config.ShowMode)
                .WriteInteger(config.Field4)
                .WriteBoolean(config.ShowOnMouseHover)
                .WriteInteger(config.ShowDuration)
                .WriteInteger(config.CategoryId)
                .WriteInteger(config.Field8)
                .WriteInteger(config.ColorId)
                .WriteInteger(config.Field10)
                .WriteInteger(config.RendererId)
                .WriteLong(config.DefaultMinValue)
                .WriteLong(config.DefaultMaxValue)
                .WriteInteger(config.Extra.Length);

            foreach (WiredVariableFxExtra extra in config.Extra)
            {
                packet.WriteString(extra.Key).WriteString(extra.Value);
            }
        }
    }
}
