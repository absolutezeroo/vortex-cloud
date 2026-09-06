using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Dances and waves, each on its own action.</summary>
public sealed class GestureTranslator : ISignalTranslator<PlayerGesturedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.Dance, [Facts.Room]), new(SignalActions.Wave, [Facts.Room])];

    public ImmutableArray<ProgressSignal> Translate(PlayerGesturedEvent e)
    {
        string? action = e.Gesture switch
        {
            "dance" => SignalActions.Dance,
            "wave" => SignalActions.Wave,
            _ => null,
        };

        return action is null
            ? []
            :
            [
                new(
                    e.PlayerId.Value,
                    action,
                    1,
                    Target: null,
                    SignalFacts.Build().Id(Facts.Room, e.RoomId)
                ),
            ];
    }
}
