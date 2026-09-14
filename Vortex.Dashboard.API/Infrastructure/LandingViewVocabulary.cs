using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Vortex.Dashboard.API.Api.Hotel.Contracts;

namespace Vortex.Dashboard.API.Infrastructure;

/// <summary>
/// What the game client will accept in the hotel view's configuration, transcribed from its own
/// source.
/// </summary>
/// <remarks>
/// Every list here is closed and every one of them fails silently. An unknown widget type leaves a
/// blank slot, an unknown element type makes <c>GenericWidget</c> abandon its whole content column,
/// an unknown layer name is never looked up, an unknown motion drops the object. Nothing is logged,
/// nothing is rejected, and the hotel simply looks wrong -- which is why these belong in the server
/// that validates a save rather than only in the dropdowns of the page.
/// <para>
/// Source of truth: <c>com.sulake.habbo.friendbar.landingview</c> in WIN63-202607011411. Re-derive
/// from there if the target client moves; the names below are the client's own strings, not ours.
/// </para>
/// </remarks>
internal static class LandingViewVocabulary
{
    /// <summary>The prefix every key this page owns begins with.</summary>
    public const string Prefix = "landing.view.";

    /// <summary>Background objects are read from index 1 up to this and no further.</summary>
    public const int MaxObjects = 20;

    /// <summary>The six dynamic slots, and there is no seventh: the client's loop stops at six.</summary>
    public const int SlotCount = 6;

    /// <summary>The 21 widget types <c>LandingViewWidgetType.getWidgetForType</c> knows.</summary>
    public static readonly FrozenSet<string> WidgetTypes = new[]
    {
        "avatarimage",
        "expiringcatalogpage",
        "expiringcatalogpagesmall",
        "communitygoal",
        "communitygoalvsmode",
        "communitygoalvsmodevote",
        "catalogpromo",
        "catalogpromosmall",
        "achievementcompetition_hall_of_fame",
        "achievementcompetition_prizes",
        "dailyquest",
        "nextlimitedrarecountdown",
        "habbomoderationpromo",
        "habbotalentspromo",
        "habbowaypromo",
        "roomhoppernetwork",
        "safetyquizpromo",
        "generic",
        "widgetcontainer",
        "promoarticle",
        "bonusrare",
    }.ToFrozenSet();

    /// <summary>The nine keys <c>GenericWidget.configureLayout</c> switches on.</summary>
    public static readonly FrozenSet<string> LayoutKeys = new[]
    {
        "bitmap.uri",
        "bitmap.width",
        "bitmap.height",
        "bitmap.x",
        "bitmap.y",
        "content.x",
        "content.y",
        "content.width",
        "container.height",
    }.ToFrozenSet();

    /// <summary>
    /// The nine background layers, in the client's own order -- which is also their paint order, so
    /// <c>background_back</c> is behind everything and <c>background_left_bottom</c> in front.
    /// </summary>
    public static readonly IReadOnlyList<string> BackgroundLayers =
    [
        "background_back",
        "background_front",
        "background_gradient_top",
        "background_hotel_top",
        "background_gradient",
        "background_right",
        "background_horizon",
        "background_left",
        "background_left_bottom",
    ];

    private static readonly FrozenSet<string> BackgroundLayerSet = BackgroundLayers.ToFrozenSet();

    /// <summary>The four motions, with the positional fields each one reads after the motion name.</summary>
    public static readonly IReadOnlyList<HotelViewMotionType> MotionTypes =
    [
        new HotelViewMotionType(
            "line",
            "reception/",
            [
                new HotelViewArgument("startX", "int", false),
                new HotelViewArgument("startY", "int", false),
                new HotelViewArgument("speedX", "number", false),
                new HotelViewArgument("speedY", "number", false),
            ]
        ),
        new HotelViewMotionType(
            "randomwalk",
            string.Empty,
            [
                new HotelViewArgument("startX", "int", false),
                new HotelViewArgument("startY", "int", false),
                new HotelViewArgument("speedX", "number", false),
                new HotelViewArgument("speedY", "number", false),
                new HotelViewArgument("driftX", "number", false),
                new HotelViewArgument("driftY", "number", false),
                new HotelViewArgument("driftIntervalMs", "int", false),
            ]
        ),
        new HotelViewMotionType(
            "spiral",
            "reception/",
            [
                new HotelViewArgument("startRadius", "int", false),
                new HotelViewArgument("startAngle", "number", false),
                new HotelViewArgument("radiusSpeed", "number", false),
                new HotelViewArgument("angleSpeed", "number", false),
                new HotelViewArgument("centerX", "number", false),
                new HotelViewArgument("centerY", "number", false),
            ]
        ),
        new HotelViewMotionType(
            "animated",
            "reception/",
            [
                new HotelViewArgument("frameCount", "int", false),
                new HotelViewArgument("fps", "int", false),
                new HotelViewArgument("x", "int", false),
                new HotelViewArgument("y", "int", false),
                new HotelViewArgument("syncToObjects", "string", true),
            ]
        ),
    ];

    private static readonly FrozenDictionary<string, HotelViewMotionType> MotionByName =
        MotionTypes.ToFrozenDictionary(motion => motion.Motion);

    /// <summary>
    /// Where each slot sits. Fixed in the client, so the page can draw the grid instead of asking the
    /// operator to hold it in their head.
    /// </summary>
    public static readonly IReadOnlyList<HotelViewSlotShape> SlotShapes =
    [
        new HotelViewSlotShape(1, "top", true, false, false),
        new HotelViewSlotShape(2, "left", true, false, false),
        new HotelViewSlotShape(3, "right", false, false, false),
        new HotelViewSlotShape(4, "left", true, true, false),
        new HotelViewSlotShape(5, "right", false, true, true),
        new HotelViewSlotShape(6, "bottom", true, false, false),
    ];

    /// <summary>
    /// The 24 element types, with the arguments each handler reads.
    /// </summary>
    /// <remarks>
    /// The four marked unverified are real types the client dispatches, whose later arguments are
    /// bound up with community-goal and competition state this hotel does not drive yet. They are
    /// listed so an existing configuration survives a save, and edited as a raw comma list, because
    /// inventing names for fields nobody has read is worse than showing the string.
    /// </remarks>
    public static readonly IReadOnlyList<HotelViewElementType> ElementTypes = BuildElements();

    private static readonly FrozenDictionary<string, HotelViewElementType> ElementByType =
        ElementTypes.ToFrozenDictionary(element => element.Type);

    public static HotelViewVocabulary Describe() =>
        new(
            [.. WidgetTypes.Order()],
            ElementTypes,
            [.. LayoutKeys.Order()],
            BackgroundLayers,
            MotionTypes,
            SlotShapes,
            MaxObjects
        );

    public static bool IsWidget(string value) => WidgetTypes.Contains(value);

    public static bool IsLayoutKey(string value) => LayoutKeys.Contains(value);

    public static bool IsBackgroundLayer(string value) => BackgroundLayerSet.Contains(value);

    public static HotelViewMotionType? Motion(string value) =>
        MotionByName.TryGetValue(value, out HotelViewMotionType? motion) ? motion : null;

    public static HotelViewElementType? Element(string value) =>
        ElementByType.TryGetValue(value, out HotelViewElementType? element) ? element : null;

    /// <summary>
    /// Whether an element's first argument is a key into external_flash_texts, and therefore whether
    /// the page offers to edit the words rather than the key.
    /// </summary>
    public static bool CarriesText(string type) =>
        Element(type) is { Verified: true, Arguments: [{ Kind: "text" }, ..] };

    private static IReadOnlyList<HotelViewElementType> BuildElements()
    {
        HotelViewArgument label = new("label", "text", false);

        return
        [
            // Text. The trailing two are read only when present, and `border` is compared to the
            // literal string "true".
            Text("caption"),
            Text("subcaption"),
            Text("bodytext"),
            new HotelViewElementType(
                "title",
                [label, new HotelViewArgument("floating", "bool", true)],
                true
            ),
            new HotelViewElementType(
                "spacing",
                [new HotelViewArgument("height", "int", false)],
                true
            ),
            // Buttons. All of them take their caption in the first argument; what differs is where
            // they send the player.
            new HotelViewElementType(
                "catalogbutton",
                [label, new HotelViewArgument("catalogPage", "catalogPage", false)],
                true
            ),
            new HotelViewElementType(
                "promotedroombutton",
                [label, new HotelViewArgument("categoryCode", "string", false)],
                true
            ),
            new HotelViewElementType(
                "link",
                [label, new HotelViewArgument("url", "url", false)],
                true
            ),
            new HotelViewElementType(
                "internallinkbutton",
                [label, new HotelViewArgument("link", "string", false)],
                true
            ),
            new HotelViewElementType(
                "gotoroombutton",
                [label, new HotelViewArgument("roomId", "roomId", false)],
                true
            ),
            new HotelViewElementType(
                "gotocompetitionroombutton",
                [label, new HotelViewArgument("roomId", "roomId", false)],
                true
            ),
            new HotelViewElementType("gotohomeroombutton", [label], true),
            new HotelViewElementType("credithabbletbutton", [label], true),
            new HotelViewElementType("buyvipbutton", [label], true),
            new HotelViewElementType("communitygoaltimer", [label], true),
            Badge("requestbadgebutton", label),
            Badge("requestbadgebuttonsecond", label),
            Badge("requestbadgebuttonthird", label),
            Badge("requestbadgebuttonfourth", label),
            Badge("requestbadgebuttonfifth", label),
            // Images and counters.
            new HotelViewElementType(
                "image",
                [
                    new HotelViewArgument("uri", "image", false),
                    new HotelViewArgument("x", "int", false),
                ],
                true
            ),
            new HotelViewElementType(
                "concurrentusersmeter",
                [
                    new HotelViewArgument("uri", "image", false),
                    new HotelViewArgument("x", "int", true),
                    new HotelViewArgument("y", "int", true),
                ],
                true
            ),
            new HotelViewElementType(
                "concurrentusersinfo",
                [label, new HotelViewArgument("uri", "image", false)],
                true
            ),
            // The countdown's first field is a flag, not a caption -- the only element where it is
            // not, and the reason `CarriesText` asks the vocabulary instead of assuming.
            new HotelViewElementType(
                "customtimer",
                [
                    new HotelViewArgument("floating", "bool", false),
                    new HotelViewArgument("x", "int", false),
                    new HotelViewArgument("y", "int", false),
                    new HotelViewArgument("remainingLabel", "text", false),
                    new HotelViewArgument("expiredLabel", "text", false),
                    new HotelViewArgument("endsAt", "string", false),
                ],
                true
            ),
            new HotelViewElementType("rewardbadge", [], false),
            new HotelViewElementType("submitcompetitionroom", [], false),
            new HotelViewElementType("dailyquest", [], false),
            new HotelViewElementType("communitygoalscore", [], false),
        ];

        static HotelViewElementType Text(string type) =>
            new(
                type,
                [
                    new HotelViewArgument("label", "text", false),
                    new HotelViewArgument("width", "int", true),
                    new HotelViewArgument("border", "bool", true),
                ],
                true
            );

        static HotelViewElementType Badge(string type, HotelViewArgument label) =>
            new(
                type,
                [
                    label,
                    new HotelViewArgument("badgeRequestCode", "badge", false),
                    new HotelViewArgument("x", "int", false),
                    new HotelViewArgument("y", "int", false),
                    new HotelViewArgument("hideWhenOwned", "bool", true),
                ],
                true
            );
    }
}
