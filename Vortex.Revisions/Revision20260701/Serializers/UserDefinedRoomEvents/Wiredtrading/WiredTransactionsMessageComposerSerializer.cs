using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredtrading;

/// <summary>
/// A page of transaction log.
/// </summary>
/// <remarks>
/// Two longs in here, both easy to write as ints and both fatal if you do: the list id in the
/// header and the transaction id on every row. The client reads them with <c>readLong</c>, so a
/// four-byte write shifts everything after it — the whole page, not just that field.
/// </remarks>
internal class WiredTransactionsMessageComposerSerializer(int header)
    : AbstractSerializer<WiredTransactionsMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        WiredTransactionsMessageComposer message
    )
    {
        WiredTransactionsSnapshot page = message.Page;

        packet
            .WriteInteger(page.LogListType)
            .WriteLong(page.LogListId)
            .WriteInteger(page.TotalLogs)
            .WriteInteger(page.CurrentPage)
            .WriteInteger(page.Amount)
            .WriteInteger(page.Logs.Length);

        foreach (WiredTransactionSnapshot log in page.Logs)
        {
            packet.WriteTransaction(log);
        }
    }
}
