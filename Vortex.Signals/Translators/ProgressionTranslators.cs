using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>
/// Habbicons used.
/// </summary>
/// <remarks>
/// The room is omitted rather than emitted as zero when the Habbicon was used in a private
/// conversation. A filter fails closed on an absent fact, so "in any room but yours" correctly does
/// not match a private conversation — which <c>room = 0</c> would have matched.
/// </remarks>
public sealed class HabbiconUsedTranslator : ISignalTranslator<HabbiconUsedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.UseHabbicon, [Facts.Habbicon, Facts.Room], TargetKind: FactKind.OpaqueId)];

    public ImmutableArray<ProgressSignal> Translate(HabbiconUsedEvent e) =>
        [
            new(
                e.PlayerId.Value,
                SignalActions.UseHabbicon,
                1,
                Target: SignalValue.Id(e.HabbiconId),
                SignalFacts.Build().Id(Facts.Habbicon, e.HabbiconId).IdIfAny(Facts.Room, e.RoomId)
            ),
        ];
}

/// <summary>Habbicon collections completed. The target is the collection code, so a task can name one.</summary>
public sealed class HabbiconCollectionTranslator
    : ISignalTranslator<HabbiconCollectionCompletedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.CompleteHabbiconCollection, [Facts.Collection], TargetKind: FactKind.Text)];

    public ImmutableArray<ProgressSignal> Translate(HabbiconCollectionCompletedEvent e) =>
        [
            new(
                e.PlayerId.Value,
                SignalActions.CompleteHabbiconCollection,
                1,
                Target: e.CollectionCode,
                SignalFacts.Build().Text(Facts.Collection, e.CollectionCode)
            ),
        ];
}

/// <summary>
/// A pet reached a new level.
/// </summary>
/// <remarks>
/// The amount is the level reached, not one: the client's own task is "get a pet to level N", which
/// is a Highest-mode task over the level. A counter-mode task on this action would add levels
/// together, which is content's mistake to make and not this translator's to prevent. The credit
/// goes to the pet's owner, who need not be whoever fed it.
/// </remarks>
public sealed class PetLevelTranslator : ISignalTranslator<PetLeveledUpEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.PetLevel, [Facts.Pet], TargetKind: FactKind.OpaqueId)];

    public ImmutableArray<ProgressSignal> Translate(PetLeveledUpEvent e) =>
        [
            new(
                e.OwnerId.Value,
                SignalActions.PetLevel,
                e.Level,
                Target: SignalValue.Id(e.PetId),
                SignalFacts.Build().Id(Facts.Pet, e.PetId)
            ),
        ];
}

/// <summary>
/// Quests completed.
/// </summary>
/// <remarks>
/// One progression system feeding another: the quest system raises this, and reward tracks consume
/// it. A consumer must never map an action back onto the system that produces it, or a completion
/// would feed itself.
/// </remarks>
public sealed class QuestCompletedTranslator : ISignalTranslator<QuestCompletedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.CompleteQuest, [])];

    public ImmutableArray<ProgressSignal> Translate(QuestCompletedEvent e) =>
        [new(e.PlayerId.Value, SignalActions.CompleteQuest, 1, Target: null, [])];
}

/// <summary>Achievement level-ups. The other progression system feeding this one.</summary>
public sealed class AchievementLevelTranslator : ISignalTranslator<AchievementLevelUpEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.AchievementLevel, [])];

    public ImmutableArray<ProgressSignal> Translate(AchievementLevelUpEvent e) =>
        [new(e.PlayerId.Value, SignalActions.AchievementLevel, 1, Target: null, [])];
}
