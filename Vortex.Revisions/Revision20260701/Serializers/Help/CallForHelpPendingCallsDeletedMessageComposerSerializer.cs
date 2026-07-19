using Vortex.Primitives.Messages.Outgoing.Help;
using Vortex.Primitives.Packets;

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
