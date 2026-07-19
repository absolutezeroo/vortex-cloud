using Vortex.Primitives.Messages.Outgoing.NewNavigator;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.NewNavigator;

internal class NewNavigatorPreferencesMessageSerializer(int header)
    : AbstractSerializer<NewNavigatorPreferencesMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        NewNavigatorPreferencesMessageComposer message
    )
    {
        packet.WriteInteger(message.WindowX);
        packet.WriteInteger(message.WindowY);
        packet.WriteInteger(message.WindowWidth);
        packet.WriteInteger(message.WindowHeight);
        packet.WriteBoolean(message.LeftPaneHidden);
        packet.WriteInteger(message.ResultsMode);
    }
}
