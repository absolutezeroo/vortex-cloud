using System;
using FluentAssertions;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Rooms.Wired;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// Every operator the "change variable" dropdown offers. The failure modes worth pinning are the
/// ones a room would show as a corrupt number rather than as an error: an overflow that wraps into
/// a negative score, a divide by zero, and an operator this revision does not know.
/// </summary>
public sealed class WiredVariableArithmeticTests
{
    private static readonly Random Rng = new(1);

    [Theory]
    [InlineData(WiredVariableOperation.Assign, 10, 3, 3)]
    [InlineData(WiredVariableOperation.Add, 10, 3, 13)]
    [InlineData(WiredVariableOperation.Subtract, 10, 3, 7)]
    [InlineData(WiredVariableOperation.Multiply, 10, 3, 30)]
    [InlineData(WiredVariableOperation.Divide, 10, 3, 3)]
    [InlineData(WiredVariableOperation.Power, 2, 10, 1024)]
    [InlineData(WiredVariableOperation.Modulo, 10, 3, 1)]
    [InlineData(WiredVariableOperation.BitwiseAnd, 0b1100, 0b1010, 0b1000)]
    [InlineData(WiredVariableOperation.BitwiseOr, 0b1100, 0b1010, 0b1110)]
    [InlineData(WiredVariableOperation.BitwiseXor, 0b1100, 0b1010, 0b0110)]
    [InlineData(WiredVariableOperation.LeftShift, 1, 4, 16)]
    [InlineData(WiredVariableOperation.RightShift, 16, 4, 1)]
    public void Applies(WiredVariableOperation operation, int current, int operand, int expected)
    {
        WiredVariableArithmetic
            .TryApply(current, operation, operand, Rng, out int result)
            .Should()
            .BeTrue();

        result.Should().Be(expected);
    }

    [Theory]
    // A floor and a cap, not "take the smaller/larger of the two" -- the labels read
    // "Set minimum" / "Set maximum".
    [InlineData(WiredVariableOperation.SetMinimum, 3, 10, 10)]
    [InlineData(WiredVariableOperation.SetMinimum, 30, 10, 30)]
    [InlineData(WiredVariableOperation.SetMaximum, 30, 10, 10)]
    [InlineData(WiredVariableOperation.SetMaximum, 3, 10, 3)]
    public void Bounds(WiredVariableOperation operation, int current, int operand, int expected)
    {
        WiredVariableArithmetic
            .TryApply(current, operation, operand, Rng, out int result)
            .Should()
            .BeTrue();

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(WiredVariableOperation.AbsoluteValue, -7, 7)]
    [InlineData(WiredVariableOperation.AbsoluteValue, 7, 7)]
    [InlineData(WiredVariableOperation.BitwiseNot, 0, -1)]
    [InlineData(WiredVariableOperation.BitCount, 0b1011, 3)]
    public void UnaryOperations_IgnoreTheOperand(
        WiredVariableOperation operation,
        int current,
        int expected
    )
    {
        WiredVariableArithmetic.RequiresOperand(operation).Should().BeFalse();

        WiredVariableArithmetic
            .TryApply(current, operation, 999, Rng, out int result)
            .Should()
            .BeTrue();

        result.Should().Be(expected);
    }

    [Fact]
    public void DivideOrModuloByZero_LeavesTheValueAlone()
    {
        WiredVariableArithmetic
            .TryApply(10, WiredVariableOperation.Divide, 0, Rng, out int divided)
            .Should()
            .BeFalse();

        divided.Should().Be(10);

        WiredVariableArithmetic
            .TryApply(10, WiredVariableOperation.Modulo, 0, Rng, out int remainder)
            .Should()
            .BeFalse();

        remainder.Should().Be(10);
    }

    [Fact]
    public void Overflow_Saturates_RatherThanWrappingNegative()
    {
        WiredVariableArithmetic
            .TryApply(int.MaxValue, WiredVariableOperation.Add, 10, Rng, out int added)
            .Should()
            .BeTrue();

        added.Should().Be(int.MaxValue);

        WiredVariableArithmetic
            .TryApply(int.MaxValue, WiredVariableOperation.Multiply, 2, Rng, out int multiplied)
            .Should()
            .BeTrue();

        multiplied.Should().Be(int.MaxValue);

        WiredVariableArithmetic
            .TryApply(2, WiredVariableOperation.Power, 62, Rng, out int raised)
            .Should()
            .BeTrue();

        raised.Should().Be(int.MaxValue);
    }

    [Fact]
    public void DivideThatWouldOverflow_Saturates()
    {
        // int.MinValue / -1 has no int answer and throws if computed in int.
        WiredVariableArithmetic
            .TryApply(int.MinValue, WiredVariableOperation.Divide, -1, Rng, out int result)
            .Should()
            .BeTrue();

        result.Should().Be(int.MaxValue);
    }

    [Fact]
    public void NegativeExponent_LeavesTheValueAlone()
    {
        WiredVariableArithmetic
            .TryApply(2, WiredVariableOperation.Power, -1, Rng, out int result)
            .Should()
            .BeFalse();

        result.Should().Be(2);
    }

    [Fact]
    public void Random_StaysWithinTheBound_AndHandlesAnEmptyRange()
    {
        for (int i = 0; i < 50; i++)
        {
            WiredVariableArithmetic
                .TryApply(0, WiredVariableOperation.RandomWithUpperBound, 6, Rng, out int result)
                .Should()
                .BeTrue();

            result.Should().BeInRange(0, 5);
        }

        WiredVariableArithmetic
            .TryApply(9, WiredVariableOperation.RandomWithUpperBound, 0, Rng, out int empty)
            .Should()
            .BeTrue();

        empty.Should().Be(0);
    }

    [Fact]
    public void UnknownOperation_LeavesTheValueAlone()
    {
        // The client's dropdown offers ids 111-118 that this revision does not name.
        WiredVariableArithmetic
            .TryApply(10, (WiredVariableOperation)111, 3, Rng, out int result)
            .Should()
            .BeFalse();

        result.Should().Be(10);
    }
}
