using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Game.Directory;

namespace Vortex.Revisions.Revision20260701.Serializers.Game.Directory;

internal class Game2StartCounterMessageMessageComposerSerializer(int header)
    : AbstractSerializer<Game2StartCounterMessageMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        Game2StartCounterMessageMessageComposer message
    )
    {
        //
    }
}
