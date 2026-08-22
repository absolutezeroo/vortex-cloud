using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredtrading;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents.Wiredtrading;

/// <summary>
/// The chest screen's deposit button.
/// </summary>
/// <remarks>
/// Consumed so it stops being logged as an unknown header, and no further: the message carries the
/// chest id alone, with no amount and no item, so what the official server answers cannot be read
/// off the client. Guessing here would put a number in someone's chest. Close it with a capture of
/// the official server, or with the client screen that follows it.
/// </remarks>
public class DepositToWiredChestMessageHandler : IMessageHandler<DepositToWiredChestMessage>
{
    public ValueTask HandleAsync(
        DepositToWiredChestMessage message,
        MessageContext ctx,
        CancellationToken ct
    ) => ValueTask.CompletedTask;
}
