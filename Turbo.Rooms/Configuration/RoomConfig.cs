using Turbo.Primitives.Rooms.Enums;
using Turbo.Primitives.Rooms.Object;

namespace Turbo.Rooms.Configuration;

public class RoomConfig
{
    public const string SECTION_NAME = "Turbo:Rooms";

    public Altitude MaxStackHeight { get; init; } = Altitude.FromInt(4000);
    public RoomScaleType DefaultRoomScale { get; init; } = RoomScaleType.Normal;
    public int DefaultWallHeight { get; init; } = 0;
    public Altitude MaxStepHeight { get; init; } = Altitude.FromInt(200);
    public bool PlaceItemsOnAvatars { get; init; } = true;
    public bool EnableDiagonalChecking { get; init; } = true;

    public int RoomCheckMs { get; init; } = 300000;
    public int RoomDeactivationDelayMs { get; init; } = 1800000;
    public int RoomTickMs { get; init; } = 50;
    public int AvatarTickMs { get; init; } = 500;
    public int PetTickMs { get; init; } = 500;
    public int PetWanderIdleMinMs { get; init; } = 6000;
    public int PetWanderIdleMaxMs { get; init; } = 14000;
    public int PetWanderRadius { get; init; } = 5;
    public int PetWanderCandidateAttempts { get; init; } = 12;
    public int PetNutritionCap { get; init; } = 100;
    public int PetEnergyCap { get; init; } = 100;
    public double PetNutritionDecayPerMinute { get; init; } = 1.0;
    public double PetEnergyDecayPerMinute { get; init; } = 0.5;
    public int PetStatFlushIntervalMs { get; init; } = 60_000;
    public int PetSleepWakeEnergyThreshold { get; init; } = 40;
    public double PetNestEnergyMultiplier { get; init; } = 2.0;
    public string PetNestLogicName { get; init; } = "furniture_pet_nest";
    public int RollerTickMs { get; init; } = 2000;
    public int WiredTickMs { get; init; } = 50;
    public int DirtyItemsTickMs { get; init; } = 2000;
    public int MaxDirtyItemsPerFlush { get; init; } = 100;
    public int MaxTileHeightsPerFlush { get; init; } = 200;
    public int MaxPathNodes { get; init; } = 4096;

    public int WiredMaxDepth { get; init; } = 20;
    public int WiredMaxScheduledPerTick { get; init; } = 64;
    public int WiredMaxEventsPerTick { get; init; } = 64;
    public int WiredSelectorMaxAreaSize { get; init; } = 100;
    public int WiredSelectedItemsLimit { get; init; } = 20;
    public bool WiredAllowWallFurni { get; init; } = true;
    public int WiredMaxIntParams { get; init; } = 16;
    public int WiredNeighborhoodRadius { get; init; } = 5;
}
