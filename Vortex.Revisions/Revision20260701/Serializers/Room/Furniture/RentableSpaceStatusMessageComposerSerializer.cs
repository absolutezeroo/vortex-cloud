using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Room.Furniture;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Furniture;

internal class RentableSpaceStatusMessageComposerSerializer(int header)
    : AbstractSerializer<RentableSpaceStatusMessageComposer>(header)
{
    /// <remarks>
    ///     Six fields, and the composer's <c>CurrencyName</c> is deliberately not one of them. The
    ///     WIN63 parser reads rented, the error code, the renter's id and name, the time remaining
    ///     and the price, and then stops — Arcturus reads the same six and Nitro five. Nobody reads a
    ///     currency name here, so writing one put a string on the wire on every rentable-space update
    ///     that every client discarded.
    ///     <para>
    ///     Trailing, so removing it shifts nothing: a reader that stopped at six was already ignoring
    ///     it. The property stays on the composer and the snapshot — the grain resolves it and the
    ///     price is meaningless without knowing the currency — it simply has no place in this
    ///     revision's wire format.
    ///     </para>
    /// </remarks>
    protected override void Serialize(
        IServerPacket packet,
        RentableSpaceStatusMessageComposer message
    )
    {
        packet
            .WriteBoolean(message.Rented)
            .WriteInteger((int)message.CanRentErrorCode)
            .WriteInteger(message.RenterId)
            .WriteString(message.RenterName)
            .WriteInteger(message.TimeRemaining)
            .WriteInteger(message.Price);
    }
}
