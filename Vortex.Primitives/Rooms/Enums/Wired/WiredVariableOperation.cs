namespace Vortex.Primitives.Rooms.Enums.Wired;

/// <summary>
/// What the "change variable" action does to the value it is pointed at. The codes are the client's
/// dropdown ids, which are deliberately sparse: 0-6 are the plain arithmetic the form shows by
/// default, and everything above is behind its "advanced" flag.
/// </summary>
/// <remarks>
/// The client's dropdown also offers ids 111-118, which no localization in this revision names.
/// They are accepted on the wire and treated as "leave the value alone" rather than rejected, so a
/// box configured against a future server still saves and simply does nothing here.
/// </remarks>
public enum WiredVariableOperation
{
    Assign = 0,
    Add = 1,
    Subtract = 2,
    Multiply = 3,
    Divide = 4,
    Power = 5,
    Modulo = 6,

    /// <summary>Raises the value to the operand when it is below it — a floor, not "take the
    /// smaller of the two".</summary>
    SetMinimum = 40,

    /// <summary>Caps the value at the operand when it is above it.</summary>
    SetMaximum = 41,
    RandomWithUpperBound = 50,
    AbsoluteValue = 60,
    BitwiseAnd = 100,
    BitwiseOr = 101,
    BitwiseXor = 102,
    BitwiseNot = 103,
    LeftShift = 104,
    RightShift = 105,
    BitCount = 110,
}
