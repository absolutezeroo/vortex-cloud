using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Avatar;

namespace Vortex.Revisions.Revision20260701.Serializers.Avatar;

internal class CheckUserNameResultMessageComposerSerializer(int header)
    : AbstractSerializer<CheckUserNameResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CheckUserNameResultMessageComposer message
    )
    {
        packet
            .WriteInteger(message.ResultCode)
            .WriteString(message.Name)
            .WriteInteger(message.NameSuggestions.Length);

        foreach (string suggestion in message.NameSuggestions)
        {
            packet.WriteString(suggestion);
        }
    }
}
