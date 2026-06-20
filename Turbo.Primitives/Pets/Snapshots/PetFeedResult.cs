using Orleans;
using Turbo.Primitives.Rooms.Object;

namespace Turbo.Primitives.Pets.Snapshots;

[GenerateSerializer, Immutable]
public sealed record PetFeedResult
{
    [Id(0)]
    public required bool Success { get; init; }

    [Id(1)]
    public required PetSnapshot? Pet { get; init; }

    [Id(2)]
    public required RoomObjectId FoodItemId { get; init; }

    [Id(3)]
    public required int NutritionAdded { get; init; }

    [Id(4)]
    public required int NutritionBefore { get; init; }

    [Id(5)]
    public required int NutritionAfter { get; init; }

    public static PetFeedResult Failed(RoomObjectId foodItemId) =>
        new()
        {
            Success = false,
            Pet = null,
            FoodItemId = foodItemId,
            NutritionAdded = 0,
            NutritionBefore = 0,
            NutritionAfter = 0,
        };
}
