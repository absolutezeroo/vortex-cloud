using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Callforhelp;

namespace Vortex.Revisions.Revision20260701.Serializers.CallForHelp;

internal class CfhSanctionMessageComposerSerializer(int header)
    : AbstractSerializer<CfhSanctionMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, CfhSanctionMessageComposer message)
    {
        //
    }
}
