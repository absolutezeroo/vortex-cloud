using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredtrading;

/// <summary>
/// The summary line of a transaction, which two messages carry.
/// </summary>
/// <remarks>
/// Shared rather than written twice: the log page and the details of one row read it with the same
/// client code, so a field added to one and not the other desynchronises whichever was forgotten.
/// Two longs in here, both easy to write as ints and both fatal if you do.
/// </remarks>
internal static class WiredTransactionWriter
{
    public static IServerPacket WriteTransaction(
        this IServerPacket packet,
        WiredTransactionSnapshot log
    ) =>
        packet
            .WriteLong(log.TransactionId)
            .WriteInteger(log.RoomId)
            .WriteInteger(log.TransactionType)
            .WriteString(log.DefinitionInfo)
            .WriteInteger(log.PlayerId)
            .WriteString(log.PlayerName)
            .WriteLong(log.Timestamp)
            .WriteString(log.ReadableTimestamp)
            .WriteInteger(log.ChestCount)
            .WriteInteger(log.WithdrawFurniCount)
            .WriteInteger(log.DepositFurniCount)
            .WriteInteger(log.WithdrawCoinsCount)
            .WriteInteger(log.DepositCoinsCount);
}
