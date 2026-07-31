using System;
using System.Collections.Generic;
using FluentAssertions;
using Vortex.Rooms.Grains.Systems;
using Xunit;

namespace Vortex.Rooms.Tests.Pets;

/// <summary>
/// A pet dropped from the inventory arrives as (0, 0) because the client has no tile to name yet.
/// Reading that literally meant the corner of the room, so placement failed whenever the corner was
/// blocked -- which is most rooms -- and a player who did not own the room could not place a pet at
/// all. These pin down that (0, 0) means "anywhere that works" while any other tile is taken as
/// aimed.
/// </summary>
public sealed class PetDropTileTests
{
    private const int Width = 10;
    private const int Height = 10;
    private const int DoorX = 4;
    private const int DoorY = 6;

    [Fact]
    public void AimedTile_IsHonouredWhenFree()
    {
        bool resolved = Resolve(7, 2, Free(), out int x, out int y);

        resolved.Should().BeTrue();
        (x, y).Should().Be((7, 2));
    }

    [Fact]
    public void AimedTile_IsRefusedWhenBlocked()
    {
        bool resolved = Resolve(7, 2, Free(blocked: (7, 2)), out _, out _);

        // Pointing at an occupied tile is a mistake the player can see and correct; it must not be
        // silently moved somewhere else.
        resolved.Should().BeFalse();
    }

    [Fact]
    public void InventoryDrop_LandsOnTheDoorTileWhenItIsFree()
    {
        bool resolved = Resolve(0, 0, Free(), out int x, out int y);

        resolved.Should().BeTrue();
        (x, y).Should().Be((DoorX, DoorY));
    }

    [Fact]
    public void InventoryDrop_StepsOutwardWhenTheDoorIsBlocked()
    {
        bool resolved = Resolve(0, 0, Free(blocked: (DoorX, DoorY)), out int x, out int y);

        resolved.Should().BeTrue();
        (x, y).Should().NotBe((DoorX, DoorY));

        // Still adjacent: the search widens a ring at a time rather than jumping across the room.
        Math.Max(Math.Abs(x - DoorX), Math.Abs(y - DoorY)).Should().Be(1);
    }

    [Fact]
    public void InventoryDrop_SucceedsWhenOnlyACornerIsFree()
    {
        bool resolved = Resolve(0, 0, OnlyFree((9, 9)), out int x, out int y);

        resolved.Should().BeTrue();
        (x, y).Should().Be((9, 9));
    }

    [Fact]
    public void InventoryDrop_SucceedsWhenTileZeroZeroIsTheBlockedOne()
    {
        // The exact case that used to fail: the literal reading of (0, 0) hit a blocked corner and
        // gave up, even with the whole room free.
        bool resolved = Resolve(0, 0, Free(blocked: (0, 0)), out int x, out int y);

        resolved.Should().BeTrue();
        (x, y).Should().NotBe((0, 0));
    }

    [Fact]
    public void InventoryDrop_FailsWhenTheRoomIsFull()
    {
        bool resolved = Resolve(0, 0, (_, _) => false, out _, out _);

        resolved.Should().BeFalse();
    }

    private static bool Resolve(
        int x,
        int y,
        Func<int, int, bool> isFree,
        out int resolvedX,
        out int resolvedY
    ) =>
        RoomPetRuntime.TryResolveDropTile(
            x,
            y,
            DoorX,
            DoorY,
            Width,
            Height,
            isFree,
            out resolvedX,
            out resolvedY
        );

    /// <summary>Every in-bounds tile is free except the ones named.</summary>
    private static Func<int, int, bool> Free(params (int X, int Y)[] blocked)
    {
        HashSet<(int, int)> taken = [.. blocked];

        return (x, y) => InBounds(x, y) && !taken.Contains((x, y));
    }

    private static Func<int, int, bool> OnlyFree((int X, int Y) tile) =>
        (x, y) => InBounds(x, y) && (x, y) == tile;

    private static bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
}
