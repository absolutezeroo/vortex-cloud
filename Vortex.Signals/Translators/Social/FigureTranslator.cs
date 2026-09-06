using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Figure changes.</summary>
public sealed class FigureTranslator : ISignalTranslator<PlayerFigureChangedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.ChangeFigure, [])];

    public ImmutableArray<ProgressSignal> Translate(PlayerFigureChangedEvent e) =>
        [new(e.PlayerId.Value, SignalActions.ChangeFigure, 1, Target: null, [])];
}
