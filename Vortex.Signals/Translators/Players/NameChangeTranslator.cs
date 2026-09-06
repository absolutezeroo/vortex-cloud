using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Changed name. The new one is filterable; the old one is history, not progress.</summary>
public sealed class NameChangeTranslator : ISignalTranslator<PlayerNameChangedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.ChangeName, [Facts.GivenName])];

    public ImmutableArray<ProgressSignal> Translate(PlayerNameChangedEvent e) =>
        [
            new(
                e.PlayerId.Value,
                SignalActions.ChangeName,
                1,
                Target: null,
                SignalFacts.Build().Text(Facts.GivenName, e.NewName)
            ),
        ];
}
