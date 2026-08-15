using System;
using System.Numerics;
using Vortex.Primitives.Rooms.Enums.Wired;

namespace Vortex.Rooms.Wired;

/// <summary>
/// The arithmetic behind the "change variable" action, kept apart from the box so every operator can
/// be pinned by a test rather than discovered in a room.
/// </summary>
/// <remarks>
/// Everything computes in <see cref="long"/> and saturates back into the int a wired variable holds:
/// a multiply that overflows should peg at the maximum, not silently wrap to a negative number in
/// the middle of someone's score.
/// </remarks>
public static class WiredVariableArithmetic
{
    /// <summary>The three operators whose form hides the operand box, and for which the client
    /// forces the value-or-variable choice to 0.</summary>
    public static bool RequiresOperand(WiredVariableOperation operation) =>
        operation
            is not (
                WiredVariableOperation.AbsoluteValue
                or WiredVariableOperation.BitwiseNot
                or WiredVariableOperation.BitCount
            );

    /// <summary>
    /// The new value, or false when the operation leaves the variable untouched: an unknown
    /// operator, or a division or modulo by zero, which must not throw inside a wired tick and has
    /// no sensible answer to write.
    /// </summary>
    /// <param name="random">Source for <see cref="WiredVariableOperation.RandomWithUpperBound"/>;
    /// pass a seeded instance to make that operator testable.</param>
    public static bool TryApply(
        int current,
        WiredVariableOperation operation,
        int operand,
        Random random,
        out int result
    )
    {
        result = current;

        switch (operation)
        {
            case WiredVariableOperation.Assign:
                result = operand;

                return true;
            case WiredVariableOperation.Add:
                result = Clamp((long)current + operand);

                return true;
            case WiredVariableOperation.Subtract:
                result = Clamp((long)current - operand);

                return true;
            case WiredVariableOperation.Multiply:
                result = Clamp((long)current * operand);

                return true;
            case WiredVariableOperation.Divide:
                if (operand == 0)
                {
                    return false;
                }

                // long division so int.MinValue / -1 saturates instead of overflowing.
                result = Clamp((long)current / operand);

                return true;
            case WiredVariableOperation.Power:
                return TryPower(current, operand, out result);
            case WiredVariableOperation.Modulo:
                if (operand == 0)
                {
                    return false;
                }

                result = Clamp((long)current % operand);

                return true;
            case WiredVariableOperation.SetMinimum:
                result = Math.Max(current, operand);

                return true;
            case WiredVariableOperation.SetMaximum:
                result = Math.Min(current, operand);

                return true;
            case WiredVariableOperation.RandomWithUpperBound:
                // Exclusive of the bound, the way every "upper bound" random is: a bound of 6 gives
                // the six outcomes 0..5. A bound of zero or less has no range to draw from.
                result = operand > 0 ? random.Next(operand) : 0;

                return true;
            case WiredVariableOperation.AbsoluteValue:
                result = current == int.MinValue ? int.MaxValue : Math.Abs(current);

                return true;
            case WiredVariableOperation.BitwiseAnd:
                result = current & operand;

                return true;
            case WiredVariableOperation.BitwiseOr:
                result = current | operand;

                return true;
            case WiredVariableOperation.BitwiseXor:
                result = current ^ operand;

                return true;
            case WiredVariableOperation.BitwiseNot:
                result = ~current;

                return true;
            case WiredVariableOperation.LeftShift:
                result = current << operand;

                return true;
            case WiredVariableOperation.RightShift:
                result = current >> operand;

                return true;
            case WiredVariableOperation.BitCount:
                result = BitOperations.PopCount((uint)current);

                return true;
            default:
                return false;
        }
    }

    /// <summary>Integer power. A negative exponent has no integer answer, so it leaves the value
    /// alone rather than collapsing it to zero.</summary>
    private static bool TryPower(int current, int exponent, out int result)
    {
        result = current;

        if (exponent < 0)
        {
            return false;
        }

        long value = 1;

        for (int i = 0; i < exponent; i++)
        {
            value *= current;

            if (value is > int.MaxValue or < int.MinValue)
            {
                result = value > 0 ? int.MaxValue : int.MinValue;

                return true;
            }
        }

        result = (int)value;

        return true;
    }

    private static int Clamp(long value) =>
        value > int.MaxValue ? int.MaxValue
        : value < int.MinValue ? int.MinValue
        : (int)value;
}
