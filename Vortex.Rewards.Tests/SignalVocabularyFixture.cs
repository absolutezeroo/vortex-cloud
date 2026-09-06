using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Vortex.Primitives.Signals;
using Vortex.Signals.Translators;

namespace Vortex.Rewards.Tests;

/// <summary>
/// The vocabulary the real translators declare, for tests that used to read the hand-kept map.
/// </summary>
/// <remarks>
/// Built by reflection over the shipped translator assembly rather than by listing shapes here. That
/// matters more than convenience: these sequence tests assert that content naming a fact an action
/// does not emit is refused, and if the fixture declared its own shapes it would be asserting
/// against itself. Reading the real ones means the tests fail when a translator stops emitting
/// something content depends on.
/// </remarks>
internal static class SignalVocabularyFixture
{
    public static ISignalVocabulary Real { get; } = Build();

    private static ISignalVocabulary Build()
    {
        ImmutableArray<SignalShape>.Builder shapes = ImmutableArray.CreateBuilder<SignalShape>();

        foreach (Type type in typeof(RoomEntryTranslator).Assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface)
            {
                continue;
            }

            Type? closed = type.GetInterfaces()
                .FirstOrDefault(i =>
                    i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISignalTranslator<>)
                );

            if (closed is null)
            {
                continue;
            }

            InterfaceMapping map = type.GetInterfaceMap(closed);
            MethodInfo getter = map.TargetMethods.Single(m =>
                m.Name.EndsWith("get_Shapes", StringComparison.Ordinal)
            );

            shapes.AddRange((ImmutableArray<SignalShape>)getter.Invoke(null, null)!);
        }

        return new FixedVocabulary(shapes.ToImmutable());
    }

    private sealed class FixedVocabulary(ImmutableArray<SignalShape> shapes) : ISignalVocabulary
    {
        public ImmutableArray<SignalShape> Shapes => shapes;

        public ImmutableArray<SignalShape> ShapesFor(string action) =>
            [.. shapes.Where(s => string.Equals(s.Action, action, StringComparison.Ordinal))];

        public bool Emits(string action, string factKey) =>
            ShapesFor(action)
                .Any(s =>
                    (s.TargetKind is not null && factKey == Facts.TargetKey)
                    || s.Facts.Any(f => string.Equals(f.Key, factKey, StringComparison.Ordinal))
                );

        public FactKey? FactFor(string action, string factKey) =>
            ShapesFor(action)
                .SelectMany(s => s.Facts)
                .FirstOrDefault(f => string.Equals(f.Key, factKey, StringComparison.Ordinal));
    }
}
