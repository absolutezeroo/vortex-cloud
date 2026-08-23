using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Preferences;

namespace Vortex.Revisions.Revision20260701.Serializers.Preferences;

/// <summary>
/// Wire layout from the WIN63-202607011411 client's own parser
/// (<c>_SafePkg_4205.GetCustomFilterResultMessageEventParser</c>, one of the few this dump leaves
/// unobfuscated): a count followed by that many strings, and nothing else.
/// </summary>
internal class GetCustomFilterResultMessageComposerSerializer(int header)
    : AbstractSerializer<GetCustomFilterResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        GetCustomFilterResultMessageComposer message
    )
    {
        packet.WriteInteger(message.Words.Length);

        foreach (string word in message.Words)
        {
            packet.WriteString(word);
        }
    }
}
