using System;

namespace Vortex.Primitives.MysteryBox;

/// <summary>
/// Maps a box colour onto the furniture states that actually draw it.
///
/// The mystery box sprite carries no recolour data: <c>mystery_box.nitro</c> declares no
/// <c>colors</c> block, and furnidata has no <c>mystery_box*N</c> variants, so neither the palette
/// route (<c>RoomContentLoader.getObjectColorIndex</c>, which reads the <c>*N</c> classname suffix)
/// nor a tint can colour it. Instead the asset ships 25 states: state 0 is the neutral, colourless
/// box, followed by eight groups of three — one group per colour, in the exact order the client's
/// <c>MysteryBoxToolbarExtension.KEY_COLORS</c> declares them. Verified by decoding the atlas and
/// sampling each frame's dominant colour:
/// <code>
///   1-3   purple      10-12  yellow     19-21  turquoise
///   4-6   blue        13-15  lilac      22-24  red
///   7-9   green       16-18  orange
/// </code>
/// Leaving the state at 0 is why an unset box renders white whatever colour it was registered as.
/// </summary>
public static class MysteryBoxSprite
{
    /// <summary>The colourless box the asset ships as state 0.</summary>
    public const int NeutralState = 0;

    /// <summary>States per colour: closed, waiting, open.</summary>
    private const int StatesPerColor = 3;

    /// <summary>Closed box in <paramref name="color"/>; <see cref="NeutralState"/> when the colour is
    /// not one the client can render.</summary>
    public static int ClosedState(string? color) => StateFor(color, 0);

    /// <summary>The second frame of a colour's group, used while the box is waiting for its other
    /// half so the room can see it is in play.</summary>
    public static int WaitingState(string? color) => StateFor(color, 1);

    /// <summary>The taller, lid-raised frame of a colour's group.</summary>
    public static int OpenState(string? color) => StateFor(color, 2);

    /// <summary>
    /// The colour a box is currently wearing, read back from its state. The state is the single
    /// source of truth for a box's colour -- it is per furniture instance, already persisted in stuff
    /// data and already on the wire -- so one <c>mystery_box</c> definition covers all eight colours.
    /// Empty for state 0 or anything outside the eight groups.
    /// </summary>
    public static string ColorFromState(int state)
    {
        if (state < 1)
        {
            return string.Empty;
        }

        int index = (state - 1) / StatesPerColor;

        return index < MysteryBoxColors.All.Length ? MysteryBoxColors.All[index] : string.Empty;
    }

    private static int StateFor(string? color, int offset)
    {
        int index = MysteryBoxColors.All.IndexOf(MysteryBoxColors.Normalize(color));

        return index < 0 ? NeutralState : 1 + (index * StatesPerColor) + offset;
    }
}
