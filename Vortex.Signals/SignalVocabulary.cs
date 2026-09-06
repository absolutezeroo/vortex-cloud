using System;
using System.Collections.Immutable;
using System.Threading;
using Vortex.Primitives.Signals;

namespace Vortex.Signals;

/// <summary>
/// Everything the loaded translators say they can produce.
/// </summary>
/// <remarks>
/// <para>
/// Filled by <see cref="SignalTranslatorFeatureProcessor"/> as assemblies are scanned, and emptied
/// again when a plugin is unloaded. It replaces <c>RewardTrackActionFacts</c>: the same question,
/// answered by the code that actually emits the facts rather than by a file kept in step by hand.
/// </para>
/// <para>
/// Registrations happen at startup and at plugin load, reads happen on every editor request and
/// every content validation. The state is therefore an immutable snapshot swapped under a lock:
/// readers never lock and never see a half-built vocabulary.
/// </para>
/// </remarks>
public sealed class SignalVocabulary : ISignalVocabulary
{
    private readonly Lock _writeLock = new();
    private ImmutableArray<SignalShape> _shapes = [];
    private ImmutableDictionary<string, ImmutableArray<SignalShape>> _byAction =
        ImmutableDictionary.Create<string, ImmutableArray<SignalShape>>(StringComparer.Ordinal);

    public ImmutableArray<SignalShape> Shapes => _shapes;

    public ImmutableArray<SignalShape> ShapesFor(string action) =>
        _byAction.TryGetValue(action, out ImmutableArray<SignalShape> shapes) ? shapes : [];

    public bool Emits(string action, string factKey)
    {
        foreach (SignalShape shape in ShapesFor(action))
        {
            // The target is republished as a fact by the host, so an action that has one can be
            // filtered on it even though no translator lists it among its facts.
            if (
                shape.TargetKind is not null
                && string.Equals(factKey, Facts.TargetKey, StringComparison.Ordinal)
            )
            {
                return true;
            }

            foreach (FactKey fact in shape.Facts)
            {
                if (string.Equals(fact.Key, factKey, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public FactKey? FactFor(string action, string factKey)
    {
        foreach (SignalShape shape in ShapesFor(action))
        {
            if (
                shape.TargetKind is FactKind targetKind
                && string.Equals(factKey, Facts.TargetKey, StringComparison.Ordinal)
            )
            {
                return new FactKey(
                    Facts.TargetKey,
                    targetKind,
                    "rewardTracks.fact_target",
                    "Target"
                );
            }

            foreach (FactKey fact in shape.Facts)
            {
                if (string.Equals(fact.Key, factKey, StringComparison.Ordinal))
                {
                    return fact;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Adds one translator's shapes, and hands back the disposable that removes them.
    /// </summary>
    /// <remarks>
    /// The removal is what makes a plugin unload leave nothing behind: the feature processor returns
    /// it in the same batch as the handler registration, so the shapes and the handler that produces
    /// them always disappear together.
    /// </remarks>
    internal IDisposable Register(ImmutableArray<SignalShape> shapes)
    {
        lock (_writeLock)
        {
            Swap(_shapes.AddRange(shapes));
        }

        return new Registration(this, shapes);
    }

    private void Remove(ImmutableArray<SignalShape> shapes)
    {
        lock (_writeLock)
        {
            ImmutableArray<SignalShape> remaining = _shapes;

            foreach (SignalShape shape in shapes)
            {
                remaining = remaining.Remove(shape);
            }

            Swap(remaining);
        }
    }

    /// <summary>Rebuilds the index and publishes both fields as one visible state.</summary>
    private void Swap(ImmutableArray<SignalShape> shapes)
    {
        ImmutableDictionary<string, ImmutableArray<SignalShape>>.Builder builder =
            ImmutableDictionary.CreateBuilder<string, ImmutableArray<SignalShape>>(
                StringComparer.Ordinal
            );

        foreach (SignalShape shape in shapes)
        {
            builder[shape.Action] = builder.TryGetValue(
                shape.Action,
                out ImmutableArray<SignalShape> existing
            )
                ? existing.Add(shape)
                : [shape];
        }

        _byAction = builder.ToImmutable();
        _shapes = shapes;
    }

    private sealed class Registration(SignalVocabulary owner, ImmutableArray<SignalShape> shapes)
        : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            owner.Remove(shapes);
        }
    }
}
