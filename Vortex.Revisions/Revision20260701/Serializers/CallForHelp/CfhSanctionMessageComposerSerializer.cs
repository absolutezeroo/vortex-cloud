using Vortex.Primitives.Messages.Outgoing.Callforhelp;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.CallForHelp;

internal class CfhSanctionMessageComposerSerializer(int header)
    : AbstractSerializer<CfhSanctionMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, CfhSanctionMessageComposer message)
    {
        //
    }
}
