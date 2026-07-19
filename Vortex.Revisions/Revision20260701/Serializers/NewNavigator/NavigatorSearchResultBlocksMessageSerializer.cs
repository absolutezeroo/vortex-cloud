using Vortex.Primitives.Messages.Outgoing.NewNavigator;
using Vortex.Primitives.Orleans.Snapshots.Navigator;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.NewNavigator.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.NewNavigator;

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
