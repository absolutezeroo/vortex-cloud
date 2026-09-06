using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Changed an account preference. The setting is named, so one task can mean one toggle.</summary>
public sealed class PreferenceTranslator : ISignalTranslator<AccountPreferenceChangedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.ChangePreference, [Facts.Section], TargetKind: FactKind.Text)];

    public ImmutableArray<ProgressSignal> Translate(AccountPreferenceChangedEvent e) =>
        [
            new(
                e.PlayerId.Value,
                SignalActions.ChangePreference,
                1,
                e.Setting,
                SignalFacts.Build().Text(Facts.Section, e.Setting)
            ),
        ];
}
