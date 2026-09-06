using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>
/// A player left a room, and how long they stayed.
/// </summary>
/// <remarks>
/// The duration is the point. "Spend ten minutes in someone's flat" cannot be written against an
/// entry — the entry knows nothing about how long it will last — and it is free here because the
/// event already carries it.
/// </remarks>
public sealed class RoomLeftTranslator : ISignalTranslator<PlayerLeftRoomEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [
        new(
            SignalActions.LeaveRoom,
            [Facts.Room, Facts.DurationSeconds],
            TargetKind: FactKind.RoomId
        ),
    ];

    public ImmutableArray<ProgressSignal> Translate(PlayerLeftRoomEvent e) =>
        [
            new(
                e.PlayerId.Value,
                SignalActions.LeaveRoom,
                1,
                SignalValue.Id(e.RoomId),
                SignalFacts
                    .Build()
                    .Id(Facts.Room, e.RoomId)
                    .Number(Facts.DurationSeconds, (int)e.RoomDurationSeconds)
            ),
        ];
}
