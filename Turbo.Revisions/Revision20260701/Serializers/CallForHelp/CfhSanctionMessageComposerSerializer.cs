using Turbo.Primitives.Messages.Outgoing.Callforhelp;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260701.Serializers.CallForHelp;

internal class CfhSanctionMessageComposerSerializer(int header)
    : AbstractSerializer<CfhSanctionMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, CfhSanctionMessageComposer message)
    {
        //
    }
}
