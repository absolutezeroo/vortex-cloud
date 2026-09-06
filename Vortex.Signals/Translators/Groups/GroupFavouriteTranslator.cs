using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>
/// Made a guild the favourite one.
/// </summary>
/// <remarks>
/// The event also fires when the favourite is cleared, and that carries no guild. Clearing is not
/// choosing, so it raises nothing rather than a signal with a hole in it.
/// </remarks>
public sealed class GroupFavouriteTranslator : ISignalTranslator<GroupFavouriteChangedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.FavouriteGroup, [Facts.Group], TargetKind: FactKind.OpaqueId)];

    public ImmutableArray<ProgressSignal> Translate(GroupFavouriteChangedEvent e) =>
        e.GroupId is int groupId
            ?
            [
                new(
                    e.ActorPlayerId,
                    SignalActions.FavouriteGroup,
                    1,
                    SignalValue.Id(groupId),
                    SignalFacts.Build().Id(Facts.Group, groupId)
                ),
            ]
            : [];
}
