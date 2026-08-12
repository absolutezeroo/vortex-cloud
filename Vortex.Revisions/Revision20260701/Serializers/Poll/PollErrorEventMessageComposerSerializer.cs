using Vortex.Primitives.Messages.Outgoing.Poll;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Poll;

/// <summary>
/// Header only. The client's parser for this event returns without reading a single field, so the
/// empty body is the correct payload, not a stub.
/// </summary>
internal class PollErrorEventMessageComposerSerializer(int header)
    : AbstractSerializer<PollErrorEventMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, PollErrorEventMessageComposer message)
    {
        // Intentionally empty -- see the class remark.
    }
}
