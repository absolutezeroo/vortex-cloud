using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Sound;

namespace Vortex.PacketHandlers.Sound;

public class GetOfficialSongIdMessageHandler : IMessageHandler<GetOfficialSongIdMessage>
{
    public async ValueTask HandleAsync(
        GetOfficialSongIdMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
