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
/// trivial is the layout around them, and it fails silently in both directions:
/// <para>
/// An id is built from (band, sub-band, order) with only a 16-bit hash of the NAME as the tiebreak,
/// so two variables sharing a sub-band and an order are two adjacent numbers whose relative position
/// is decided by a hash — the picker would list them in an order nobody chose, and it would change
/// the moment either is renamed. Nothing throws.
/// </para>
/// <para>
/// And a boolean reading is spelled by NOT declaring <see cref="WiredVariableFlags.HasValue"/> and
/// answering with the return of <c>TryGetValue</c>, where a numeric one declares the flag and always
/// returns true. A leaf that declares the flag but means the boolean reads as a permanent 1; one
/// that omits it but means a number loses the number. Both compile.
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
    /// Every leaf of the band, built with no room. Only <c>GetVarSnapshot()</c> is read, and that is
    /// derived from the leaf's own declarations — the grain is not touched until something asks for
    /// a value.
    /// </summary>
    private static List<WiredVariableSnapshot> Band() =>
        [
            .. typeof(UserVariable<>)
                .Assembly.GetTypes()
                .Where(t =>
                    !t.IsAbstract
                    && t.Namespace == typeof(UserIndexVariable).Namespace
                    && typeof(IWiredInternalVariable).IsAssignableFrom(t)
                )
                .Select(t =>
                    ((WiredInternalVariable)Activator.CreateInstance(t, [null])!).GetVarSnapshot()
                ),
        ];
}
