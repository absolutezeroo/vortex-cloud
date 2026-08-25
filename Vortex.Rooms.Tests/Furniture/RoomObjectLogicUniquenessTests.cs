using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Object.Logic.Furniture;
using Vortex.Rooms.Object.Logic.Furniture.Floor;
using Xunit;

namespace Vortex.Rooms.Tests.Furniture;

/// <summary>
/// Every <c>[RoomObjectLogic]</c> key in this assembly, checked for the collision the registry now
/// refuses.
/// </summary>
/// <remarks>
/// The registry used to let a second registration replace the first silently. It fails now, which is
/// the right behaviour and also a much sharper edge: two core logics sharing a name and a family
/// would take the emulator down at startup rather than one of them quietly losing. This is the test
/// that says they do not — and the one that catches it in CI rather than on a hotel's first boot
/// after an upgrade.
/// </remarks>
public sealed class RoomObjectLogicUniquenessTests
{
    private enum Family
    {
        Any,
        Floor,
        Wall,
    }

    [Fact]
    public void NoTwoLogicsClaimTheSameNameAndFamily()
    {
        Dictionary<(string Name, Family Family), List<string>> owners = [];

        foreach (Type type in typeof(FurnitureFloorLogic).Assembly.GetTypes())
        {
            Family family = FamilyOf(type);

            foreach (
                RoomObjectLogicAttribute attribute in type.GetCustomAttributes<RoomObjectLogicAttribute>(
                    inherit: false
                )
            )
            {
                if (!owners.TryGetValue((attribute.Key, family), out List<string>? claimants))
                {
                    claimants = [];
                    owners[(attribute.Key, family)] = claimants;
                }

                claimants.Add(type.Name);
            }
        }

        string[] collisions =
        [
            .. owners
                .Where(pair => pair.Value.Count > 1)
                .Select(pair =>
                    $"{pair.Key.Name} ({pair.Key.Family}): {string.Join(", ", pair.Value)}"
                )
                .Order(),
        ];

        collisions
            .Should()
            .BeEmpty(
                "the registry refuses a colliding registration, so two logics claiming one name and "
                    + "family stops the emulator at startup rather than one of them quietly losing"
            );
    }

    private static Family FamilyOf(Type type)
    {
        if (typeof(IFurnitureWallLogic).IsAssignableFrom(type))
        {
            return Family.Wall;
        }

        return typeof(IFurnitureFloorLogic).IsAssignableFrom(type) ? Family.Floor : Family.Any;
    }
}
