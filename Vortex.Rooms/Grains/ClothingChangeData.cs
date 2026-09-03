namespace Vortex.Rooms.Grains;

/// <summary>
/// The two outfits a clothing-change booth holds, in the one string the client reads them from.
/// </summary>
/// <remarks>
/// <c>FurnitureClothingChangeLogic</c> splits the item's data on a comma and takes index 0 as the
/// boys' look and index 1 as the girls'. One <c>SetClothingChangeData</c> carries one gender, so
/// writing the string means merging into it — which is the whole reason this is not two lines inside
/// the grain.
/// </remarks>
internal static class ClothingChangeData
{
    private const char Separator = ',';

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

    private static bool IsGirls(string gender) =>
        !string.IsNullOrEmpty(gender) && (gender[0] == 'F' || gender[0] == 'f');
}
