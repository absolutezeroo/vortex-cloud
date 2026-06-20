using Turbo.Primitives.Messages.Outgoing.NewNavigator;
using Turbo.Primitives.Orleans.Snapshots.Navigator;
using Turbo.Primitives.Packets;
using Turbo.Revisions.Revision20260112.Serializers.NewNavigator.Data;

namespace Turbo.Revisions.Revision20260112.Serializers.NewNavigator;

internal class NavigatorSearchResultBlocksMessageSerializer(int header)
    : AbstractSerializer<NavigatorSearchResultBlocksMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        NavigatorSearchResultBlocksMessageComposer message
    )
    {
        packet
            .WriteString(message.SearchCodeOriginal)
            .WriteString(message.FilteringData)
            .WriteInteger(message.Blocks.Length);

        foreach (NavigatorSearchResultBlockSnapshot block in message.Blocks)
        {
            NavigatorSearchResultBlockSerializer.Serialize(packet, block);
        }
    }
}
