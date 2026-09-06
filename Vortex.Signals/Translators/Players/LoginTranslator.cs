using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Logged in. The only action a player takes without doing anything.</summary>
public sealed class LoginTranslator : ISignalTranslator<PlayerLoggedInEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } = [new(SignalActions.Login, [])];

    public ImmutableArray<ProgressSignal> Translate(PlayerLoggedInEvent e) =>
        [new(e.PlayerId, SignalActions.Login, 1, Target: null, [])];
}
