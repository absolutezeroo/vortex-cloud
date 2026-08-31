using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Fishing;

namespace Vortex.Revisions.Revision20260701.Serializers.Fishing;

/// <summary>
/// Field order here is the contract with vortex-modern-client's VortexHookHavocResultMessageParser —
/// keep the two in lockstep, and only ever append.
/// </summary>
internal class VortexHookHavocResultMessageComposerSerializer(int header)
    : AbstractSerializer<VortexHookHavocResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        VortexHookHavocResultMessageComposer message
    ) =>
        packet
            .WriteInteger(message.AttemptId)
            .WriteBoolean(message.Won)
            .WriteInteger(message.SpeciesId)
            .WriteInteger(message.XpGained)
            .WriteInteger(message.CurrencyGained)
            .WriteInteger(message.TrophyHandItemId);
}
