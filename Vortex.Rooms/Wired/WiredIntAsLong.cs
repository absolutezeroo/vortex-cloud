namespace Vortex.Rooms.Wired;

/// <summary>
/// The client writes a signed 64-bit wired parameter as two consecutive int params
/// (<c>Util.pushIntAsLong</c>: the sign word first, then the value). Every box carrying a numeric
/// operand — the variable comparison, the variable arithmetic — uses that pair, so reading the
/// second int alone silently truncates anything that ever exceeded the int range.
/// </summary>
public static class WiredIntAsLong
{
    /// <summary>Recombines the pair the client pushed. <paramref name="high"/> is 0 for a positive
    /// value and -1 for a negative one, which is exactly the top half of the two's-complement
    /// long, so this is the plain reassembly and not a special case.</summary>
    public static long Read(int high, int low) => ((long)high << 32) | (uint)low;

    /// <summary>The same value brought back into the int range wired values actually live in
    /// (<c>WiredVariableValue</c> is an int), saturating rather than wrapping so an out-of-range
    /// operand keeps its sign and its "very large" meaning in a comparison.</summary>
    public static int ReadClamped(int high, int low)
    {
        long value = Read(high, low);

        return value > int.MaxValue ? int.MaxValue
            : value < int.MinValue ? int.MinValue
            : (int)value;
    }
}
