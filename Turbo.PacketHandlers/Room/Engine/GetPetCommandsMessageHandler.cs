using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Room.Engine;
using Turbo.Primitives.Messages.Outgoing.Room.Pets;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Pets.Providers;
using Turbo.Primitives.Pets.Snapshots;
using Turbo.Primitives.Rooms.Grains;

namespace Turbo.PacketHandlers.Room.Engine;

public class GetPetCommandsMessageHandler(
    IGrainFactory grainFactory,
    IPetCommandProvider petCommandProvider
) : IMessageHandler<GetPetCommandsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly IPetCommandProvider _petCommandProvider = petCommandProvider;

    public async ValueTask HandleAsync(
        GetPetCommandsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0 || message.PetId <= 0)
        {
            return;
        }

        IRoomGrain room = _grainFactory.GetRoomGrain(ctx.RoomId);
        PetSnapshot? pet = await room.GetPlacedPetSnapshotAsync(message.PetId, ct)
            .ConfigureAwait(false);

        if (pet is null)
        {
            return;
        }

        ImmutableArray<int> allIds = _petCommandProvider.GetAllCommandIds(pet.Type);
        ImmutableArray<int> enabledIds = _petCommandProvider.GetEnabledCommandIds(
            pet.Type,
            pet.Level
        );

        await ctx.SendComposerAsync(
                new PetCommandsMessageComposer
                {
                    PetId = message.PetId,
                    AllCommandIds = allIds,
                    EnabledCommandIds = enabledIds,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
