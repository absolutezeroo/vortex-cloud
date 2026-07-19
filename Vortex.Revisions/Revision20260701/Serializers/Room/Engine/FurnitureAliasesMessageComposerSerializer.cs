using Vortex.Primitives.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Engine;

internal class FurnitureAliasesMessageComposerSerializer(int header)
    : AbstractSerializer<FurnitureAliasesMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, FurnitureAliasesMessageComposer message)
    {
        packet.WriteInteger(message.Aliases.Count);

        foreach ((string alias, string original) in message.Aliases)
        {
            packet.WriteString(alias).WriteString(original);
        }
    }
}
