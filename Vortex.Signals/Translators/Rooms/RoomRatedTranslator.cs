using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>
/// A room was rated up or down.
/// </summary>
/// <remarks>
/// The event existed and nothing consumed it — the whole "like a room" vocabulary was one
/// translator away. <c>points</c> is the direction, so "like ten rooms" filters on <c>+1</c> and
/// a task about downvotes is expressible without a second action.
/// </remarks>
public sealed class RoomRatedTranslator : ISignalTranslator<RoomRatedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.RateRoom, [Facts.Room, Facts.RatingPoints], TargetKind: FactKind.RoomId)];

    public ImmutableArray<ProgressSignal> Translate(RoomRatedEvent e) =>
        [
            new(
                e.ActorId.Value,
                SignalActions.RateRoom,
                1,
                SignalValue.Id(e.RoomId),
                SignalFacts.Build().Id(Facts.Room, e.RoomId).Number(Facts.RatingPoints, e.Points)
            ),
        ];
}
