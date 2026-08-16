using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredtrading;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents.Wiredtrading;

/// <summary>
/// The client telling the room it closed a chest.
/// </summary>
/// <remarks>
/// Nothing to undo yet: opening a chest takes no lock and reserves nothing, so closing one is only
/// news. The handler exists so the message is consumed rather than logged as unknown, and it is
/// where a future reservation would be released.
/// </remarks>
public class CloseWiredChestMessageHandler : IMessageHandler<CloseWiredChestMessage>
{
    public ValueTask HandleAsync(
        CloseWiredChestMessage message,
        MessageContext ctx,
        CancellationToken ct
    ) => ValueTask.CompletedTask;
}
