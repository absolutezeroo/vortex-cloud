using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Wired.Variables;
using Vortex.Rooms.Wired.Variables.User;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// What the User band as a whole has to hold, whatever leaves it grows.
/// </summary>
/// <remarks>
/// The readings themselves are one field access each and are not worth a test apiece. What is not
/// trivial is the layout around them, and it fails silently in three directions:
/// <para>
/// An id is built from (band, sub-band, order) with only a 16-bit hash of the NAME as the tiebreak,
/// so two variables sharing a sub-band and an order are two adjacent numbers whose relative position
/// is decided by a hash — the picker would list them in an order nobody chose, and it would change
/// the moment either is renamed. Nothing throws.
/// </para>
/// <para>
/// A boolean reading is spelled by NOT declaring <see cref="WiredVariableFlags.HasValue"/> and
/// answering with the return of <c>TryGetValue</c>, where a numeric one declares the flag and always
/// returns true. A leaf that declares the flag but means the boolean reads as a permanent 1; one
/// that omits it but means a number loses the number. Both compile.
/// </para>
/// <para>
/// And <see cref="WiredVariableFlags.CanWriteValue"/> is the single thing the "change variable value"
/// box filters its picker on, so declaring it without a write puts the reading in front of a builder
/// and then ignores them. <c>@type</c> and <c>@position.x</c> both shipped that way.
/// </para>
/// </remarks>
public sealed class WiredUserVariableBandTests
{
    [Fact]
    public void NoTwoReadingsClaimTheSamePlaceInThePicker()
    {
        IEnumerable<IGrouping<WiredVariableId, WiredVariableSnapshot>> collisions = Band()
            .GroupBy(v => v.VariableId)
            .Where(g => g.Count() > 1);

        collisions
            .Should()
            .BeEmpty(
                "an order shared inside a sub-band leaves the picker's order to a name hash: {0}",
                string.Join(
                    ", ",
                    collisions.Select(g => string.Join(" / ", g.Select(v => v.VariableName)))
                )
            );
    }

    [Fact]
    public void EveryReadingIsEitherANumberOrAFlag_NeverBoth_AndNeverNeither()
    {
        foreach (WiredVariableSnapshot variable in Band())
        {
            bool isNumber = variable.Flags.Has(WiredVariableFlags.HasValue);
            bool isFlag = variable.Flags == WiredVariableFlags.None;

            (isNumber ^ isFlag)
                .Should()
                .BeTrue(
                    "{0} must either carry a value or be a bare flag, and it declares {1}",
                    variable.VariableName,
                    variable.Flags
                );
        }
    }

    [Fact]
    public void NothingClaimsToBeWritableWithoutAWriteBehindIt()
    {
        List<string> liars =
        [
            .. Leaves()
                .Where(leaf =>
                    Snapshot(leaf).Flags.Has(WiredVariableFlags.CanWriteValue) && !Writes(leaf)
                )
                .Select(leaf => Snapshot(leaf).VariableName),
        ];

        liars
            .Should()
            .BeEmpty(
                "the change-value box filters its picker on exactly this flag, so each of these "
                    + "would be offered to a builder and then silently ignore them: {0}",
                string.Join(", ", liars)
            );
    }

    /// <summary>
    /// What a builder is offered in the "change variable value" box, pinned by name. It is a golden
    /// list on purpose: this set IS the box's contents, so growing or shrinking it is a visible change
    /// to the room editor and should be a deliberate line in a diff rather than a surprise.
    /// <para>
    /// It also keeps the assertion above honest — a <c>Writes</c> that answered true for everything
    /// would satisfy an empty-liars check and fail this one.
    /// </para>
    /// </summary>
    [Fact]
    public void TheWritableReadingsAreTheOnesWithARoomOperationBehindThem()
    {
        Band()
            .Where(v => v.Flags.Has(WiredVariableFlags.CanWriteValue))
            .Select(v => v.VariableName)
            .Should()
            .BeEquivalentTo([
                "@effect",
                "@handitem",
                "@dance",
                "@sign",
                "@direction",
                "@position.x",
                "@position.y",
                "@team",
            ]);
    }

    [Fact]
    public void TheWholeBandTargetsUsers()
    {
        Band()
            .Should()
            .OnlyContain(v => v.TargetType == WiredVariableTargetType.User)
            .And.HaveCountGreaterThan(
                1,
                "a reflection sweep that finds nothing would pass every other assertion here"
            );
    }

    /// <summary>
    /// Whether this leaf really overrides the write, stopping short of <c>UserVariable&lt;&gt;</c>
    /// itself — that is where the refusing default lives, and counting it would make every leaf look
    /// like it writes.
    /// <para>
    /// The walk matters in the other direction too: <c>@position.x</c> and <c>@position.y</c> declare
    /// nothing of their own and inherit a real write from <c>UserPositionVariable</c>.
    /// </para>
    /// </summary>
    private static bool Writes(Type leaf)
    {
        for (Type? type = leaf; type is not null; type = type.BaseType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(UserVariable<>))
            {
                return false;
            }

            if (
                type.GetMethod(
                    "SetValueForAvatarAsync",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly
                )
                is not null
            )
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Every concrete leaf of the band.</summary>
    private static List<Type> Leaves() =>
        [
            .. typeof(UserVariable<>)
                .Assembly.GetTypes()
                .Where(t =>
                    !t.IsAbstract
                    && t.Namespace == typeof(UserIndexVariable).Namespace
                    && typeof(IWiredInternalVariable).IsAssignableFrom(t)
                ),
        ];

    /// <summary>
    /// One leaf's declarations, built with no room. Only <c>GetVarSnapshot()</c> is read, and that is
    /// derived from what the leaf declares — the grain is not touched until something asks for a
    /// value.
    /// </summary>
    private static WiredVariableSnapshot Snapshot(Type leaf) =>
        ((WiredInternalVariable)Activator.CreateInstance(leaf, [null])!).GetVarSnapshot();

    private static List<WiredVariableSnapshot> Band() => [.. Leaves().Select(Snapshot)];
}
