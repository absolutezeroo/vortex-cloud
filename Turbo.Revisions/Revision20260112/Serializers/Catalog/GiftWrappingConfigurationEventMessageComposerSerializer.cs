using Turbo.Primitives.Messages.Outgoing.Catalog;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Serializers.Catalog;

internal class GiftWrappingConfigurationEventMessageComposerSerializer(int header)
    : AbstractSerializer<GiftWrappingConfigurationEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        GiftWrappingConfigurationEventMessageComposer message
    )
    {
        var config = message.Configuration;
        packet.WriteBoolean(config.IsWrappingEnabled);
        packet.WriteInteger(config.WrappingPrice);

        packet.WriteInteger(config.StuffTypes.Length);
        foreach (var id in config.StuffTypes)
            packet.WriteInteger(id);

        packet.WriteInteger(config.BoxTypes.Length);
        foreach (var id in config.BoxTypes)
            packet.WriteInteger(id);

        packet.WriteInteger(config.RibbonTypes.Length);
        foreach (var id in config.RibbonTypes)
            packet.WriteInteger(id);

        packet.WriteInteger(config.DefaultStuffTypes.Length);
        foreach (var id in config.DefaultStuffTypes)
            packet.WriteInteger(id);
    }
}
