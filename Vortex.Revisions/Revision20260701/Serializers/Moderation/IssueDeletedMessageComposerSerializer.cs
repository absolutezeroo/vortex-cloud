using System.Globalization;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Moderation;

namespace Vortex.Revisions.Revision20260701.Serializers.Moderation;

internal class IssueDeletedMessageComposerSerializer(int header)
    : AbstractSerializer<IssueDeletedMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, IssueDeletedMessageComposer message) =>
        // Not an int: the client does parseInt(readString()) on this field.
        packet.WriteString(message.IssueId.ToString(CultureInfo.InvariantCulture));
}
