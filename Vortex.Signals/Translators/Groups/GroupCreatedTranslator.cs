using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Founded a guild. The name it was given is filterable; so is the room it was tied to.</summary>
public sealed class GroupCreatedTranslator : ISignalTranslator<GroupCreatedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [
        new(
            SignalActions.CreateGroup,
            [Facts.Group, Facts.GivenName, Facts.Room, Facts.Price],
            TargetKind: FactKind.OpaqueId
        ),
    ];

    public ImmutableArray<ProgressSignal> Translate(GroupCreatedEvent e) =>
        [
            new(
                e.ActorPlayerId,
                SignalActions.CreateGroup,
                1,
                SignalValue.Id(e.GroupId),
                SignalFacts
                    .Build()
                    .Id(Facts.Group, e.GroupId)
                    .Text(Facts.GivenName, e.GroupName)
                    .IdIfAny(Facts.Room, e.RoomId)
                    .Number(Facts.Price, e.CreditCost)
            ),
        ];
}
