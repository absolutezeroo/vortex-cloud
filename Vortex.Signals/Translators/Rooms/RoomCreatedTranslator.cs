using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>
/// Rooms created.
/// </summary>
/// <remarks>
/// The facts are what the creation form said — the name, the blurb, the category it was filed under
/// and the model it was built on. Those are what "build a flat in Chill" is made of; the room id is
/// only there so a later step can say "in it", since nobody can know the id of a room that does not
/// exist yet.
/// </remarks>
public sealed class RoomCreatedTranslator : ISignalTranslator<RoomCreatedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [
        new(
            SignalActions.CreateRoom,
            [Facts.Room, Facts.RoomName, Facts.RoomDescription, Facts.Category, Facts.Model],
            TargetKind: FactKind.RoomId
        ),
    ];

    public ImmutableArray<ProgressSignal> Translate(RoomCreatedEvent e) =>
        [
            new(
                e.OwnerId.Value,
                SignalActions.CreateRoom,
                1,
                Target: SignalValue.Id(e.RoomId),
                SignalFacts
                    .Build()
                    .Id(Facts.Room, e.RoomId)
                    .Text(Facts.RoomName, e.Name)
                    .Text(Facts.RoomDescription, e.Description)
                    .IdIfAny(Facts.Category, e.CategoryId)
                    .Text(Facts.Model, e.ModelName)
            ),
        ];
}
