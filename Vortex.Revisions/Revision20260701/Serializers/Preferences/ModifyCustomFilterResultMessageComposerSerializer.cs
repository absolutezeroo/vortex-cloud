using Vortex.Primitives.Messages.Outgoing.Preferences;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Preferences;

/// <summary>
/// Wire layout from the WIN63-202607011411 client's own parser
/// (<c>_SafePkg_4205.ModifyCustomFilterResultMessageEventParser</c>): the result code, then the
/// word it applied to. The client keys its list on the word, which is why it travels back.
/// </summary>
internal class ModifyCustomFilterResultMessageComposerSerializer(int header)
    : AbstractSerializer<ModifyCustomFilterResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        ModifyCustomFilterResultMessageComposer message
    )
    {
        packet.WriteInteger((int)message.Result).WriteString(message.Word);
    }
}
