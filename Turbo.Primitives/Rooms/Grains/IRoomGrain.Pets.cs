using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Turbo.Primitives.Action;
using Turbo.Primitives.Pets.Snapshots;
using Turbo.Primitives.Rooms.Enums;
using Turbo.Primitives.Rooms.Object;

namespace Turbo.Primitives.Rooms.Grains;

public partial interface IRoomGrain
{
    public Task<PetSnapshot?> PlacePetAsync(
        ActionContext ctx,
        int petId,
        int x,
        int y,
        Rotation direction,
        CancellationToken ct
    );

    public Task<PetSnapshot?> MovePetAsync(
        ActionContext ctx,
        int petId,
        int x,
        int y,
        Rotation direction,
        CancellationToken ct
    );

    public Task<PetSnapshot?> PickUpPetAsync(ActionContext ctx, int petId, CancellationToken ct);
    public Task<PetFeedResult> FeedPetAsync(
        ActionContext ctx,
        int petId,
        RoomObjectId foodItemId,
        CancellationToken ct
    );
    public Task<PetSnapshot?> GetPlacedPetSnapshotAsync(int petId, CancellationToken ct);
    public Task<ImmutableArray<PetSnapshot>> GetPlacedPetSnapshotsAsync(CancellationToken ct);
    public Task<PetSnapshot?> RespectPetAsync(ActionContext ctx, int petId, CancellationToken ct);
    public Task<PetSnapshot?> GrantPetCommandXpAsync(
        ActionContext ctx,
        int petId,
        CancellationToken ct
    );
    public Task<PetSnapshot?> GiveSupplementToPetAsync(
        ActionContext ctx,
        int petId,
        CancellationToken ct
    );

    public Task TogglePetBreedingPermissionAsync(
        ActionContext ctx,
        int petId,
        CancellationToken ct
    );

    public Task<bool> BreedPetsAsync(
        ActionContext ctx,
        int petOneId,
        int petTwoId,
        CancellationToken ct
    );

    public Task<bool> ConfirmPetBreedingAsync(ActionContext ctx, int petId, CancellationToken ct);

    public Task CancelPetBreedingAsync(ActionContext ctx, int petId, CancellationToken ct);

    public Task<PetSnapshot?> IssueCommandAsync(
        ActionContext ctx,
        int petId,
        int commandId,
        CancellationToken ct
    );

    public Task<PetSnapshot?> PlantMonsterplantSeedAsync(
        ActionContext ctx,
        RoomObjectId seedItemId,
        CancellationToken ct
    );
}
