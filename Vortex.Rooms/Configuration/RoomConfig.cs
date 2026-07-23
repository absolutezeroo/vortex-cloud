using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Rooms.Configuration;

public class RoomConfig
{
    public const string SECTION_NAME = "Vortex:Rooms";

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
    public PetConfig Pet { get; init; } = new();
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

    /// <summary>Hard cap on the wired event queue. WiredMaxEventsPerTick bounds the tick's work;
    /// this bounds memory under a sustained event storm — events past the cap are dropped and
    /// reported once per tick in the room's wired log.</summary>
    public int WiredMaxQueuedEvents { get; init; } = 512;

    /// <summary>How long a wired box stays lit after firing before the wired system reverts its
    /// visual state.</summary>
    public int WiredFlashDurationMs { get; init; } = 500;

    public int MaxVisitorsLimit { get; init; } = 50;

    public int DoorbellTimeoutMs { get; init; } = 20000;

    /// <summary>Business cap on how many items one player may offer on their side of a trade. The
    /// parser also bounds a single batch (<c>ProtocolLimitsConfig.MaxTradeItems</c>) for wire safety;
    /// this is the per-side offer ceiling enforced by the trade session.</summary>
    public int MaxTradeItemsPerSide { get; init; } = 1500;
}
