using Vortex.Primitives.Messages.Outgoing.Roomsettings;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.RoomSettings;

internal class ShowEnforceRoomCategoryDialogEventMessageComposerSerializer(int header)
    : AbstractSerializer<ShowEnforceRoomCategoryDialogEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        ShowEnforceRoomCategoryDialogEventMessageComposer message
    )
    {
        //
    }
}
