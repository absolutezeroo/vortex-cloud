using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.Sound;
using Vortex.Protocol.Messages.Outgoing.Sound;

namespace Vortex.PacketHandlers.Sound;

/// <summary>
/// Answers the Trax composer with the sound machine's playlist, which in this hotel is empty.
/// </summary>
/// <remarks>
/// Loading a sound machine is its own feature — disks go into the machine the way they go into a
/// jukebox, and none of that is built. What is built is the answer, because the alternative is
/// worse than an empty list: the client's composer waits for this message before it draws anything,
/// so silence leaves the dialog blank with no explanation, while an empty list is both true and
/// something the client already knows how to render.
/// </remarks>
public class GetSoundMachinePlayListMessageHandler : IMessageHandler<GetSoundMachinePlayListMessage>
{
    public async ValueTask HandleAsync(
        GetSoundMachinePlayListMessage message,
        MessageContext ctx,
        CancellationToken ct
    ) =>
        await ctx.SendComposerAsync(
                new PlayListMessageComposer { Songs = [], SynchronizationCountMs = 0 },
                ct
            )
            .ConfigureAwait(false);
}
