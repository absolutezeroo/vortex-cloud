using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Collectibles;

namespace Vortex.Revisions.Revision20260701.Serializers.Collectibles;

internal class CollectibleWalletAddressesMessageComposerSerializer(int header)
    : AbstractSerializer<CollectibleWalletAddressesMessageComposer>(header)
{
    // The stardust wallet is written on its own before the rest: the client pushes it into the same
    // list, but only when it is not empty, which is how it says "none linked".
    protected override void Serialize(
        IServerPacket packet,
        CollectibleWalletAddressesMessageComposer message
    )
    {
        packet
            .WriteString(message.StardustWalletAddress)
            .WriteInteger(message.WalletAddresses.Length);

        foreach (string address in message.WalletAddresses)
        {
            packet.WriteString(address);
        }
    }
}
