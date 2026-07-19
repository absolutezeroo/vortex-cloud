using Vortex.Primitives.Messages.Outgoing.Callforhelp;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.CallForHelp;

internal class SanctionStatusEventMessageComposerSerializer(int header)
    : AbstractSerializer<SanctionStatusEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        SanctionStatusEventMessageComposer message
    )
    {
        //
    }
}
