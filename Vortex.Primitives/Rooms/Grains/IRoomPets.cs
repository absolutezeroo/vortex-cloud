using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Action;
using Vortex.Primitives.Pets.Snapshots;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Rooms.Grains;

/// <summary>Pets placed in the room: placement, care, commands, riding and breeding.</summary>
[Alias("Vortex.Primitives.Rooms.Grains.IRoomPets")]
public interface IRoomPets : IGrainWithIntegerKey
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

    /// <summary>Gets a player on or off a pet; the client sends one message for both.</summary>
    public Task MountPetAsync(ActionContext ctx, int petId, bool mount, CancellationToken ct);

    public Task RemoveSaddleFromPetAsync(ActionContext ctx, int petId, CancellationToken ct);

    public Task TogglePetRidingPermissionAsync(ActionContext ctx, int petId, CancellationToken ct);

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

    /// <summary>
    /// A furniture used on a pet (the client's "use product" flow). Food and the monsterplant
    /// potions share one packet; the product's own category decides which it is.
    /// </summary>
    public Task<bool> UsePetProductAsync(
        ActionContext ctx,
        int petId,
        RoomObjectId productItemId,
        CancellationToken ct
    );

    /// <summary>
    /// Waters a monsterplant: the client's "treat" button, which arrives on the same packet as
    /// respect (576) and resets the well-being clock the client counts down from.
    /// </summary>
    public Task<PetSnapshot?> TreatPlantAsync(ActionContext ctx, int petId, CancellationToken ct);

    /// <summary>Harvests a full-grown monsterplant, handing its owner a seed and spending the
    /// plant's charge until a rebreed potion restores it.</summary>
    public Task<bool> HarvestPlantAsync(ActionContext ctx, int petId, CancellationToken ct);

    /// <summary>Composts a withered monsterplant. Destructive and irreversible, which is why the
    /// client asks for confirmation first.</summary>
    public Task<bool> CompostPlantAsync(ActionContext ctx, int petId, CancellationToken ct);
}
