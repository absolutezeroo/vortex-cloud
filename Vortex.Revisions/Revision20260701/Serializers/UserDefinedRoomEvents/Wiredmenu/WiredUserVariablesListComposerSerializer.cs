using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredmenu;

internal class WiredUserVariablesListComposerSerializer(int header)
    : AbstractSerializer<WiredUserVariablesListComposer>(header)
{
    protected override void Serialize(IServerPacket packet, WiredUserVariablesListComposer message)
    {
        WiredVariableOwnersPageSnapshot page = message.Page;

        packet
            .WriteString(page.VariableId)
            .WriteInteger(page.TotalEntries)
            .WriteInteger(page.CurrentPage)
            .WriteInteger(page.Amount)
            .WriteInteger(page.Elements.Count);

        foreach (WiredVariableOwnerEntry element in page.Elements)
        {
            packet
                .WriteInteger(element.EntityId)
                .WriteString(element.EntityName)
                .WriteInteger(element.Value);
        }

        packet.WriteInteger(page.UserTypeFilter).WriteInteger(page.SortTypeFilter);
    }
}
