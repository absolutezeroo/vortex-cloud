using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Help;

namespace Vortex.Revisions.Revision20260701.Serializers.Help;

internal class CallForHelpPendingCallsDeletedMessageComposerSerializer(int header)
    : AbstractSerializer<CallForHelpPendingCallsDeletedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CallForHelpPendingCallsDeletedMessageComposer message
    )
    {
        //
    }
}
