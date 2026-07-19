using Vortex.Primitives.Messages.Outgoing.Game.Directory;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Game.Directory;

internal class Game2StartingGameFailedMessageMessageComposerSerializer(int header)
    : AbstractSerializer<Game2StartingGameFailedMessageMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        Game2StartingGameFailedMessageMessageComposer message
    )
    {
        //
    }
}
