using Vortex.Primitives.Messages.Outgoing.Game.Directory;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Game.Directory;

internal class Game2GameNotFoundMessageMessageComposerSerializer(int header)
    : AbstractSerializer<Game2GameNotFoundMessageMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        Game2GameNotFoundMessageMessageComposer message
    )
    {
        //
    }
}
