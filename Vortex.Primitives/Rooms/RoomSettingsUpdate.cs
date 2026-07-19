using Orleans;
using Vortex.Primitives.Navigator.Enums;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Primitives.Rooms;

[GenerateSerializer, Immutable]
public sealed record RoomSettingsUpdate
{
    [Id(0)]
    public required string Name { get; init; }

    [Id(1)]
    public required string Description { get; init; }

    [Id(2)]
    public required RoomDoorModeType DoorMode { get; init; }

    [Id(3)]
    public required string Password { get; init; }

    [Id(4)]
    public required int MaxVisitors { get; init; }

    [Id(5)]
    public required int CategoryId { get; init; }

    [Id(6)]
    public required RoomTradeModeType TradeMode { get; init; }

    [Id(7)]
    public required bool AllowPets { get; init; }

    [Id(8)]
    public required bool AllowPetsEat { get; init; }

    [Id(9)]
    public required bool AllowBlocking { get; init; }

    [Id(10)]
    public required bool HideWalls { get; init; }

    [Id(11)]
    public required RoomThicknessType WallThickness { get; init; }

    [Id(12)]
    public required RoomThicknessType FloorThickness { get; init; }

    [Id(13)]
    public required ModSettingType WhoCanMute { get; init; }

    [Id(14)]
    public required ModSettingType WhoCanKick { get; init; }

    [Id(15)]
    public required ModSettingType WhoCanBan { get; init; }

    [Id(16)]
    public required ChatModeType ChatMode { get; init; }

    [Id(17)]
    public required ChatBubbleWidthType ChatBubbleSize { get; init; }

    [Id(18)]
    public required ChatScrollSpeedType ChatScrollSpeed { get; init; }

    [Id(19)]
    public required int ChatFullHearRange { get; init; }

    [Id(20)]
    public required ChatFloodSensitivityType ChatFloodSensitivity { get; init; }
}
