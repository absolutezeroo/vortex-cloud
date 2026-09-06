using System.Collections.Immutable;
using Vortex.Primitives.RewardTracks;

namespace Vortex.Primitives.Signals;

/// <summary>
/// The core fact vocabulary, typed.
/// </summary>
/// <remarks>
/// <para>
/// Every key forwards to <see cref="RewardTrackFacts"/> for the same reason
/// <see cref="SignalActions"/> forwards to <c>RewardTrackActions</c>: the strings are in the
/// database, and a second hand-written copy is a drift waiting to happen. What is new here is the
/// <see cref="FactKind"/> beside each one — the thing the dashboard used to hardcode per page.
/// </para>
/// <para>
/// <see cref="RoomOwner"/> is deliberately absent from the list an editor sees. Nothing emits it:
/// <c>PlayerEnteredRoomEvent</c> does not carry the owner, and reading it per entry would be a grain
/// call on an arrival path that has been slow before. It stays declared in
/// <see cref="RewardTrackFacts"/> so the shape is obvious to whoever plumbs it, and out of here so
/// nobody can pick a filter that never matches.
/// </para>
/// </remarks>
public static class Facts
{
    /// <summary>
    /// What the signal is mainly about. Never declared by a translator: the host republishes
    /// <see cref="ProgressSignal.Target"/> under this key, and a shape says what it means for that
    /// action through <see cref="SignalShape.TargetKind"/>.
    /// </summary>
    public const string TargetKey = RewardTrackFacts.Target;

    /// <summary>The room object id of a piece of furniture: the identity that survives a move.</summary>
    public static readonly FactKey Item = new(
        RewardTrackFacts.Item,
        FactKind.OpaqueId,
        "rewardTracks.fact_item",
        "The furniture"
    );

    /// <summary>A furniture definition id — the kind of thing, not the thing.</summary>
    public static readonly FactKey Definition = new(
        RewardTrackFacts.Definition,
        FactKind.FurnitureId,
        "rewardTracks.fact_def",
        "Furniture type"
    );

    /// <summary>Floor or wall: the coarse type no definition id can express on its own.</summary>
    public static readonly FactKey Placement = new(
        RewardTrackFacts.Placement,
        FactKind.Enum,
        "rewardTracks.fact_kind",
        "Floor or wall",
        [
            new(RewardTrackFacts.PlacementFloor, "rewardTracks.placement_floor", "Floor item"),
            new(RewardTrackFacts.PlacementWall, "rewardTracks.placement_wall", "Wall item"),
        ]
    );

    /// <summary>A room id.</summary>
    public static readonly FactKey Room = new(
        RewardTrackFacts.Room,
        FactKind.RoomId,
        "rewardTracks.fact_room",
        "Room"
    );

    /// <summary>A room's name. Worth filtering only with <c>Contains</c>.</summary>
    public static readonly FactKey RoomName = new(
        RewardTrackFacts.RoomName,
        FactKind.Text,
        "rewardTracks.fact_name",
        "Room name"
    );

    /// <summary>A room's description.</summary>
    public static readonly FactKey RoomDescription = new(
        RewardTrackFacts.RoomDescription,
        FactKind.Text,
        "rewardTracks.fact_desc",
        "Room description"
    );

    /// <summary>A navigator flat-category id.</summary>
    public static readonly FactKey Category = new(
        RewardTrackFacts.Category,
        FactKind.CategoryId,
        "rewardTracks.fact_category",
        "Category"
    );

    /// <summary>A room model name — the layout.</summary>
    public static readonly FactKey Model = new(
        RewardTrackFacts.Model,
        FactKind.Text,
        "rewardTracks.fact_model",
        "Room model"
    );

    /// <summary>Another player's id: the one respected, befriended, traded with.</summary>
    public static readonly FactKey Player = new(
        RewardTrackFacts.Player,
        FactKind.PlayerId,
        "rewardTracks.fact_player",
        "The other player"
    );

    /// <summary>A catalogue offer id.</summary>
    public static readonly FactKey Offer = new(
        RewardTrackFacts.Offer,
        FactKind.OfferId,
        "rewardTracks.fact_offer",
        "Catalogue offer"
    );

    /// <summary>A Habbicon id.</summary>
    public static readonly FactKey Habbicon = new(
        RewardTrackFacts.Habbicon,
        FactKind.OpaqueId,
        "rewardTracks.fact_habbicon",
        "Habbicon"
    );

    /// <summary>A Habbicon collection code.</summary>
    public static readonly FactKey Collection = new(
        RewardTrackFacts.Collection,
        FactKind.Text,
        "rewardTracks.fact_collection",
        "Collection"
    );

    /// <summary>A pet id.</summary>
    public static readonly FactKey Pet = new(
        RewardTrackFacts.Pet,
        FactKind.OpaqueId,
        "rewardTracks.fact_pet",
        "Pet"
    );

    /// <summary>A badge code.</summary>
    public static readonly FactKey Badge = new(
        RewardTrackFacts.Badge,
        FactKind.BadgeCode,
        "rewardTracks.fact_badge",
        "Badge"
    );

    /// <summary>Every core fact, in declaration order. The governance test freezes this list.</summary>
    public static readonly ImmutableArray<FactKey> All =
    [
        Item,
        Definition,
        Placement,
        Room,
        RoomName,
        RoomDescription,
        Category,
        Model,
        Player,
        Offer,
        Habbicon,
        Collection,
        Pet,
        Badge,
    ];
}
