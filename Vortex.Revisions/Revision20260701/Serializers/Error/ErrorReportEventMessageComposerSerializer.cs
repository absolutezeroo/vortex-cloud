using Vortex.Primitives.Messages.Outgoing.Error;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Error;

internal class ErrorReportEventMessageComposerSerializer(int header)
    : AbstractSerializer<ErrorReportEventMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, ErrorReportEventMessageComposer message)
    {
        //
    }
}
