using System.Globalization;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Players.Grains;
using Vortex.Protocol.Messages.Outgoing.Nft;

namespace Vortex.Revisions.Revision20260701.Serializers.Nft;

internal class UserNftWardrobeMessageComposerSerializer(int header)
    : AbstractSerializer<UserNftWardrobeMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, UserNftWardrobeMessageComposer message)
    {
        packet.WriteInteger(message.Avatars.Length);

        foreach (NftAvatarSnapshot avatar in message.Avatars)
        {
            // The order the client's constructor reads, which is not the order its getters are
            // declared in: id, figure, gender, token, contract. Following the getters would swap
            // gender with the figure and the token with the contract.
            packet
                .WriteString(avatar.CopyId.ToString(CultureInfo.InvariantCulture))
                .WriteString(avatar.Figure)
                .WriteString(avatar.Gender)
                .WriteString(avatar.TokenId)
                .WriteString(avatar.ContractKey);
        }
    }
}
