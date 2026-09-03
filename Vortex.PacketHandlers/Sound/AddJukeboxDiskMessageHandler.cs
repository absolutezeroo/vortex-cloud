using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Sound.Snapshots;
using Vortex.Protocol.Messages.Incoming.Sound;
using Vortex.Protocol.Messages.Outgoing.Sound;

namespace Vortex.PacketHandlers.Sound;

/// <summary>
/// Loads a song disk into the room's jukebox.
/// </summary>
/// <remarks>
/// The room does the moving and tells everyone in it what the playlist now holds — a jukebox is
/// shared, so a disk that only the loader could see would be wrong on every other screen. The one
/// reply that goes to the sender alone is the refusal for a full jukebox, which is a dialog rather
/// than a state change.
/// </remarks>
public class AddJukeboxDiskMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<AddJukeboxDiskMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        AddJukeboxDiskMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        JukeboxLoadResult result = await _grainFactory
            .GetRoomJukebox(ctx.RoomId)
            .AddDiskAsync(ctx.AsActionContext(), message.DiskItemId, ct)
            .ConfigureAwait(false);

        if (result.Outcome is JukeboxLoadOutcome.Full)
        {
            await ctx.SendComposerAsync(new JukeboxPlayListFullMessageComposer(), ct)
                .ConfigureAwait(false);
        }
    }
}
