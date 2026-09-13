namespace Vortex.Primitives.Rooms.Enums;

/// <summary>
/// The facial expression the client plays on an avatar when a chat line arrives. The value is an
/// index into the client's own gesture table — <c>["","sml","agr","srp","sad","joy","crz","tng",
/// "eyb","mis","puz"]</c> in <c>avatar/enum/_SafeCls_2652.as:100</c> — so the numbers are the
/// client's, not ours, and anything above <see cref="Puzzled"/> renders as no gesture at all.
/// </summary>
public enum AvatarGestureType
{
    None = 0,

    /// <summary>sml</summary>
    Smile = 1,

    /// <summary>agr</summary>
    Angry = 2,

    /// <summary>srp</summary>
    Surprised = 3,

    /// <summary>sad</summary>
    Sad = 4,

    /// <summary>joy</summary>
    Joy = 5,

    /// <summary>crz</summary>
    Crazy = 6,

    /// <summary>tng</summary>
    Tongue = 7,

    /// <summary>eyb</summary>
    EyeBlink = 8,

    /// <summary>mis</summary>
    Mischievous = 9,

    /// <summary>puz</summary>
    Puzzled = 10,
}
