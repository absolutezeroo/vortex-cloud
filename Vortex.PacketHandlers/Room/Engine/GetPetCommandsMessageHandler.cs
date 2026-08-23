using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Pets.Providers;
using Vortex.Primitives.Pets.Snapshots;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Protocol.Messages.Incoming.Room.Engine;
using Vortex.Protocol.Messages.Outgoing.Room.Pets;

namespace Vortex.PacketHandlers.Room.Engine;

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

        IRoomPets room = _grainFactory.GetRoomPets(ctx.RoomId);
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
