using System.Text.Json;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Snapshots.Furniture;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor;

/// <summary>
/// A wrapped gift. Two kinds of state, and only one of them is the client's business.
/// </summary>
/// <remarks>
/// What the client renders comes from map stuff data, whose keys are fixed by
/// <c>FurniturePresentLogic.setObjectVariables</c> on its side: <c>MESSAGE</c>, <c>PRODUCT_CODE</c>,
/// <c>PURCHASER_NAME</c>, <c>PURCHASER_FIGURE</c>, <c>TRUSTED_SENDER</c>. The wrapping's appearance
/// is not in there at all — box and ribbon are packed into the floor item's <c>extra</c> field as
/// <c>box * 1000 + ribbon</c>, which the gift-wrapped visualization splits back apart to pick two
/// sprite layers.
/// <para>
/// What is actually inside is deliberately NOT in stuff data: everything there reaches every client
/// in the room, so a present would announce its own contents before anybody opened it. It lives in
/// a private extra-data section instead.
/// </para>
/// </remarks>
[RoomObjectLogic("furniture_present")]
public class FurniturePresentLogic(IStuffDataFactory stuffDataFactory, IRoomFloorItemContext ctx)
    : FurnitureFloorLogic(stuffDataFactory, ctx)
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public override int GetExtra() => ReadContents()?.Wrapping ?? 0;

    /// <summary>
    /// What the present holds, or null when it holds nothing the server can resolve — an old row
    /// from before gifts were wrapped, or a present placed by the furni editor.
    /// </summary>
    public PresentContentsSnapshot? ReadContents()
    {
        if (
            !_ctx.RoomObject.ExtraData.TryGetSection(
                ExtraDataSectionType.PRESENT,
                out JsonElement element
            )
        )
        {
            return null;
        }

        try
        {
            return element.Deserialize<PresentContentsSnapshot>(ReadOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
