using Turbo.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Turbo.Primitives.Packets;
using Turbo.Primitives.Rooms.Snapshots.Wired.Variables;

namespace Turbo.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredmenu;

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
