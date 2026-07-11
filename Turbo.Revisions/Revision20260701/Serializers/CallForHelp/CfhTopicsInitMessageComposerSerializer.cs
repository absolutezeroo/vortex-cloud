using Turbo.Primitives.Messages.Outgoing.Callforhelp;
using Turbo.Primitives.Moderation;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260701.Serializers.CallForHelp;

internal class CfhTopicsInitMessageComposerSerializer(int header)
    : AbstractSerializer<CfhTopicsInitMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, CfhTopicsInitMessageComposer message)
    {
        packet.WriteInteger(message.Categories.Length);

        foreach (CfhCategorySnapshot category in message.Categories)
        {
            packet.WriteString(category.Name).WriteInteger(category.Topics.Length);

            foreach (CfhTopicSnapshot topic in category.Topics)
            {
                packet
                    .WriteString(topic.Name)
                    .WriteInteger(topic.Id)
                    .WriteString(topic.Consequence ?? string.Empty);
            }
        }
    }
}
