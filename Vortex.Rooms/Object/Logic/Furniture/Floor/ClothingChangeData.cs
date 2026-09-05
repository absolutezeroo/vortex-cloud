using System;
using System.Text;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor;

/// <summary>
/// The two outfits a clothing-change booth holds, and what putting one on does to a look.
/// </summary>
/// <remarks>
/// <para>
/// The comma-separated pair is <b>wire format, not a private encoding</b>: the client's
/// <c>FurnitureClothingChangeLogic</c> splits the item's legacy string on a comma and publishes
/// index 0 as <c>furniture_clothing_boy</c> and index 1 as <c>furniture_clothing_girl</c>. One
/// <c>SetClothingChangeData</c> carries one gender, so writing the string means merging into it —
/// which is the whole reason this is not two lines inside the grain.
/// </para>
/// <para>
/// The <b>merge</b> below is a different kind of claim. The client has no clothing-change rules at
/// all: it opens a widget and renders whichever figure the server broadcasts. Which parts of a look
/// a booth replaces is therefore known only from the open-source reference emulator's
/// <c>FigureUtil.mergeFigures</c> call — evidence, not authority — and the split it uses is the one
/// kept here: everything above the neck stays the wearer's, the outfit comes from the booth.
/// </para>
/// </remarks>
internal static class ClothingChangeData
{
    private const char Separator = ',';

    /// <summary>The parts a booth never touches — the wearer stays recognisably themselves.</summary>
    private static readonly string[] KeptFromWearer = ["hd", "hr", "ha", "he", "ea", "fa"];

    /// <summary>The parts the booth supplies: the outfit itself.</summary>
    private static readonly string[] TakenFromBooth = ["ch", "ca", "cc", "cp", "lg", "wa", "sh"];

    /// <summary>
    /// Puts <paramref name="look" /> on <paramref name="gender" />'s side of
    /// <paramref name="existing" />, leaving the other side as it was.
    /// </summary>
    /// <param name="gender">The client sends <c>M</c> or <c>F</c>; anything that is not the girls'
    /// side is treated as the boys', so an unexpected value writes somewhere rather than nowhere.</param>
    public static string Merge(string existing, string gender, string look)
    {
        string[] looks = (existing ?? string.Empty).Split(Separator);
        string boy = looks.Length > 0 ? looks[0] : string.Empty;
        string girl = looks.Length > 1 ? looks[1] : string.Empty;

        if (IsGirls(gender))
        {
            girl = look ?? string.Empty;
        }
        else
        {
            boy = look ?? string.Empty;
        }

        return $"{boy}{Separator}{girl}";
    }

    /// <summary>
    /// The outfit this booth holds for <paramref name="gender"/>, or empty when it holds none — a
    /// booth nobody has configured yet dresses nobody, rather than stripping them to their head.
    /// </summary>
    public static string LookFor(string data, AvatarGenderType gender)
    {
        string[] looks = (data ?? string.Empty).Split(Separator);
        int side = gender == AvatarGenderType.Female ? 1 : 0;

        return looks.Length > side ? looks[side] : string.Empty;
    }

    /// <summary>
    /// <paramref name="wearerLook"/> wearing <paramref name="boothLook"/>: their head, the booth's
    /// clothes. A part the booth does not carry is simply absent from the result, which is what
    /// makes a kit of only <c>ch</c> and <c>lg</c> leave the wearer barefoot rather than un-merged.
    /// </summary>
    public static string Dress(string wearerLook, string boothLook)
    {
        StringBuilder look = new();

        AppendParts(look, wearerLook, KeptFromWearer);
        AppendParts(look, boothLook, TakenFromBooth);

        return look.ToString();
    }

    private static void AppendParts(StringBuilder look, string figure, string[] wanted)
    {
        foreach (
            string part in (figure ?? string.Empty).Split(
                '.',
                StringSplitOptions.RemoveEmptyEntries
            )
        )
        {
            int dash = part.IndexOf('-', StringComparison.Ordinal);
            string type = dash < 0 ? part : part[..dash];

            if (Array.IndexOf(wanted, type) < 0)
            {
                continue;
            }

            if (look.Length > 0)
            {
                look.Append('.');
            }

            look.Append(part);
        }
    }

    private static bool IsGirls(string gender) =>
        !string.IsNullOrEmpty(gender) && (gender[0] == 'F' || gender[0] == 'f');
}
