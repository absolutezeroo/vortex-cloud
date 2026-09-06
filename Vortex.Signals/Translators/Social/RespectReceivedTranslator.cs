using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>
/// Was respected by somebody else.
/// </summary>
/// <remarks>
/// The receiving half of <c>give_respect</c>, and the running total comes free on the event — so
/// "be respected a hundred times" is a Highest-mode task rather than a counter that drifts.
/// </remarks>
public sealed class RespectReceivedTranslator : ISignalTranslator<RespectReceivedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.ReceiveRespect, [])];

    public ImmutableArray<ProgressSignal> Translate(RespectReceivedEvent e) =>
        [new(e.PlayerId, SignalActions.ReceiveRespect, 1, Target: null, [])];
}
