using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Fishing;

namespace Vortex.Revisions.Revision20260701.Serializers.Fishing;

/// <summary>
/// Field order here is the contract with vortex-modern-client's
/// VortexHookHavocStartedMessageParser — keep the two in lockstep, and only ever append.
/// </summary>
internal class VortexHookHavocStartedMessageComposerSerializer(int header)
    : AbstractSerializer<VortexHookHavocStartedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        VortexHookHavocStartedMessageComposer message
    ) =>
        packet
            .WriteInteger(message.AttemptId)
            .WriteInteger(message.Seed)
            .WriteInteger(message.DurationMs)
            .WriteInteger(message.FillRate)
            .WriteInteger(message.Tolerance);
}
