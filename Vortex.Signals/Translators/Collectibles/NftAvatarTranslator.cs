using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>
/// Wore a collectible as an avatar.
/// </summary>
/// <remarks>
/// Taking it off raises the same event with no copy, and that is not an act to reward — so it
/// raises nothing rather than a signal that says a player wore nothing.
/// </remarks>
public sealed class NftAvatarTranslator : ISignalTranslator<NftAvatarWornEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.WearNftAvatar, [], TargetKind: FactKind.OpaqueId)];

    public ImmutableArray<ProgressSignal> Translate(NftAvatarWornEvent e) =>
        e.CopyId is int copyId
            ? [new(e.PlayerId.Value, SignalActions.WearNftAvatar, 1, SignalValue.Id(copyId), [])]
            : [];
}
