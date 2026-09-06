using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>
/// A room's settings, category or tags were saved.
/// </summary>
/// <remarks>
/// This is where the room's <em>name</em> becomes filterable outside creation: the event carries it
/// on every save. <c>section</c> says which dialog did it, so "rename a flat" and "file it under a
/// category" are two different tasks on one action.
/// </remarks>
public sealed class RoomSettingsTranslator : ISignalTranslator<RoomSettingsUpdatedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [
        new(
            SignalActions.UpdateRoomSettings,
            [Facts.Room, Facts.RoomName, Facts.Section],
            TargetKind: FactKind.RoomId
        ),
    ];

    public ImmutableArray<ProgressSignal> Translate(RoomSettingsUpdatedEvent e) =>
        [
            new(
                e.ActorId.Value,
                SignalActions.UpdateRoomSettings,
                1,
                SignalValue.Id(e.RoomId),
                SignalFacts
                    .Build()
                    .Id(Facts.Room, e.RoomId)
                    .Text(Facts.RoomName, e.Name)
                    .Text(Facts.Section, e.Section)
            ),
        ];
}
