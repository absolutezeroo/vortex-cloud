using Vortex.Protocol.Messages.Outgoing.Moderation;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Moderation;

internal class SanctionInfoMessageComposerSerializer(int header)
    : AbstractSerializer<SanctionInfoMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, SanctionInfoMessageComposer message)
    {
        packet
            .WriteInteger(message.IssueId)
            .WriteInteger(message.AccountId)
            .WriteString(message.SanctionName)
            .WriteInteger(message.SanctionLengthInHours)
            // An int the client reads and never exposes through a getter; kept so the two optional
            // strings after it land at the right offset.
            .WriteInteger(0)
            .WriteBoolean(message.AvatarOnly)
            .WriteString(message.TradeLockInfo)
            .WriteString(message.MachineBanInfo);
    }
}
