using FluentAssertions;
using Vortex.Rooms.Grains.Systems;
using Xunit;

namespace Vortex.Rooms.Tests.Pets;

/// <summary>
/// A pet only walks to a bowl that still has something in it. State doubles as the serving counter,
/// which breaks down for a furni that has only one frame: its only valid state is 0, which used to
/// read as empty and made the fifteen one-state bowls -- waterbowl_basic and water_bowl1 -- invisible
/// to every pet in the hotel.
/// </summary>
public sealed class PetBowlAvailabilityTests
{
    [Theory]
    [InlineData(6, 5, true, "a full six-state bowl")]
    [InlineData(6, 1, true, "one serving left")]
    [InlineData(6, 0, false, "drained")]
    [InlineData(4, 0, false, "a drained food bowl")]
    [InlineData(1, 0, true, "one-state bowl: state 0 is the only frame it has, not an empty one")]
    [InlineData(0, 0, true, "no states declared at all, so state says nothing")]
    public void HasServingsLeft_ReadsStateOnlyWhenStateCanMeanSomething(
        int totalStates,
        int state,
        bool expected,
        string because
    )
    {
        RoomPetRuntime.HasServingsLeft(totalStates, state).Should().Be(expected, because);
    }
}
