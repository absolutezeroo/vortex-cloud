using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.RewardTracks;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Furniture placed. The target is the definition id, so a task can require a kind of furni.</summary>
public sealed class ItemPlacedTranslator : ISignalTranslator<ItemPlacedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [
        new(
            SignalActions.PlaceItem,
            [Facts.Item, Facts.Definition, Facts.Placement, Facts.Room],
            TargetKind: FactKind.FurnitureId
        ),
    ];

    public ImmutableArray<ProgressSignal> Translate(ItemPlacedEvent e) =>
        [
            new(
                e.ActorPlayerId,
                SignalActions.PlaceItem,
                1,
                Target: SignalValue.Id(e.DefinitionId),
                SignalFacts
                    .Build()
                    .Id(Facts.Item, e.ItemId)
                    .Id(Facts.Definition, e.DefinitionId)
                    .Enum(
                        Facts.Placement,
                        e.IsWallItem
                            ? RewardTrackFacts.PlacementWall
                            : RewardTrackFacts.PlacementFloor
                    )
                    .Id(Facts.Room, e.RoomId)
            ),
        ];
}

/// <summary>
/// Furniture moved within a room, and furniture turned on the spot.
/// </summary>
/// <remarks>
/// One translator where there were two handlers on the same event, and it raises <b>both</b> actions
/// for a rotation. That is deliberate and predates this design: the client has no separate rotate
/// message, so every rotation has always raised <c>ItemMovedEvent</c>, and quietly excluding
/// rotations from <c>move_item</c> would make existing tasks count less than the day they were
/// written. <c>RotatedInPlace</c> is what makes the narrower signal possible, not a
/// reclassification of the wider one.
/// </remarks>
public sealed class ItemMovedTranslator : ISignalTranslator<ItemMovedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [
        new(SignalActions.MoveItem, [Facts.Item, Facts.Room]),
        new(SignalActions.RotateItem, [Facts.Item, Facts.Room]),
    ];

    public ImmutableArray<ProgressSignal> Translate(ItemMovedEvent e)
    {
        ImmutableArray<SignalFact> facts = SignalFacts
            .Build()
            .Id(Facts.Item, e.ItemId)
            .Id(Facts.Room, e.RoomId);

        return e.RotatedInPlace
            ?
            [
                new(e.ActorPlayerId, SignalActions.MoveItem, 1, Target: null, facts),
                new(e.ActorPlayerId, SignalActions.RotateItem, 1, Target: null, facts),
            ]
            : [new(e.ActorPlayerId, SignalActions.MoveItem, 1, Target: null, facts)];
    }
}

/// <summary>
/// Furniture taken back out of a room.
/// </summary>
/// <remarks>
/// No client artwork exists for this, so it makes a poor first step and a good later one — "place
/// it, walk on it, then pick it up" is the shape it was added for.
/// </remarks>
public sealed class ItemPickedUpTranslator : ISignalTranslator<ItemPickedUpEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.PickUpItem, [Facts.Item, Facts.Room], TargetKind: FactKind.OpaqueId)];

    public ImmutableArray<ProgressSignal> Translate(ItemPickedUpEvent e) =>
        [
            new(
                e.ActorPlayerId,
                SignalActions.PickUpItem,
                1,
                Target: SignalValue.Id(e.ItemId),
                SignalFacts.Build().Id(Facts.Item, e.ItemId).Id(Facts.Room, e.RoomId)
            ),
        ];
}

/// <summary>
/// Stepping onto a piece of floor furniture.
/// </summary>
/// <remarks>
/// The highest-frequency signal in the hotel by a wide margin — once per tile walked onto, for every
/// avatar in every room. It is only affordable because the interest gate runs on the calling thread
/// before this translator is reached: with no content naming <c>walk_on_furni</c>, a walked tile is
/// one hash lookup and a return, which is what the room tick's own comment counts on.
/// </remarks>
public sealed class WalkOnFurniTranslator : ISignalTranslator<PlayerWalkedOnFurniEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [
        new(
            SignalActions.WalkOnFurni,
            [Facts.Item, Facts.Definition, Facts.Room],
            TargetKind: FactKind.OpaqueId
        ),
    ];

    public ImmutableArray<ProgressSignal> Translate(PlayerWalkedOnFurniEvent e) =>
        [
            new(
                e.PlayerId,
                SignalActions.WalkOnFurni,
                1,
                Target: SignalValue.Id(e.ItemId),
                SignalFacts
                    .Build()
                    .Id(Facts.Item, e.ItemId)
                    .Id(Facts.Definition, e.DefinitionId)
                    .Id(Facts.Room, e.RoomId)
            ),
        ];
}
