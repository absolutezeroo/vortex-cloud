using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Primitives.Rooms;

public partial interface IRoomService
{
    public Task PlacePetInRoomAsync(
        ActionContext ctx,
        int petId,
        int x,
        int y,
        Rotation direction,
        CancellationToken ct
    );

    public Task MovePetInRoomAsync(
        ActionContext ctx,
        int petId,
        int x,
        int y,
        Rotation direction,
        CancellationToken ct
    );

    public Task PickUpPetInRoomAsync(ActionContext ctx, int petId, CancellationToken ct);
}
