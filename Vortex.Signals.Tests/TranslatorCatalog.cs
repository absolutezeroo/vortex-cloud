using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Vortex.Primitives.Events;
using Vortex.Primitives.Players;
using Vortex.Primitives.Signals;
using Vortex.Signals.Translators;
using Xunit;

namespace Vortex.Signals.Tests;

/// <summary>One translator, reduced to what a test needs to drive it.</summary>
public sealed record TranslatorUnderTest(
    Type Concrete,
    Type EventType,
    ImmutableArray<SignalShape> Shapes,
    Func<IEvent, ImmutableArray<ProgressSignal>> Translate,
    Func<IEvent, string> DeliveryIdOf
)
{
    public string Name => Concrete.Name;

    public override string ToString() => Name;
}

/// <summary>
/// Finds every translator the way the runtime does, and builds an event to feed it.
/// </summary>
public static class TranslatorCatalog
{
    /// <summary>Every translator in the shipped assembly.</summary>
    public static ImmutableArray<TranslatorUnderTest> All { get; } = Discover();

    /// <summary>As xunit member data.</summary>
    public static TheoryData<TranslatorUnderTest> AsTheoryData()
    {
        TheoryData<TranslatorUnderTest> data = [];

        foreach (TranslatorUnderTest translator in All)
        {
            data.Add(translator);
        }

        return data;
    }

    private static ImmutableArray<TranslatorUnderTest> Discover()
    {
        ImmutableArray<TranslatorUnderTest>.Builder builder =
            ImmutableArray.CreateBuilder<TranslatorUnderTest>();

        foreach (Type type in typeof(RoomEntryTranslator).Assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition)
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

            Type eventType = closed.GetGenericArguments()[0];
            object instance = Activator.CreateInstance(type)!;
            InterfaceMapping map = type.GetInterfaceMap(closed);

            MethodInfo shapesGetter = map.TargetMethods.Single(m =>
                m.Name.EndsWith("get_Shapes", StringComparison.Ordinal)
            );
            MethodInfo translate = map.TargetMethods.Single(m =>
                m.Name.EndsWith("Translate", StringComparison.Ordinal)
            );
            MethodInfo deliveryId = map.TargetMethods.Single(m =>
                m.Name.EndsWith("DeliveryIdOf", StringComparison.Ordinal)
            );

            builder.Add(
                new TranslatorUnderTest(
                    type,
                    eventType,
                    (ImmutableArray<SignalShape>)shapesGetter.Invoke(null, null)!,
                    e => (ImmutableArray<ProgressSignal>)translate.Invoke(instance, [e])!,
                    e => (string)deliveryId.Invoke(instance, [e])!
                )
            );
        }

        return builder.ToImmutable();
    }
}

/// <summary>
/// Builds a domain event from its positional constructor.
/// </summary>
/// <remarks>
/// <para>
/// No fixture generator is in the repository, and this needs to cover exactly the ten types the
/// translated events use — so it is sixty lines rather than a dependency.
/// </para>
/// <para>
/// Every value is non-null, non-empty and non-zero on purpose. A zero id or an empty string would
/// pass straight through a translator that forgot to read a field, and the structural test would go
/// green on a translation that emits nothing. An unknown type throws with its name rather than
/// producing a default, because a silently defaulted field is the same failure one level down.
/// </para>
/// </remarks>
public static class EventFixture
{
    public static IEvent Build(
        Type eventType,
        IReadOnlyDictionary<string, object?>? overrides = null
    )
    {
        ConstructorInfo ctor = eventType
            .GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .First();

        object?[] args =
        [
            .. ctor.GetParameters()
                .Select(p =>
                    overrides is not null && overrides.TryGetValue(p.Name!, out object? given)
                        ? given
                        : Value(p.ParameterType, p.Name!)
                ),
        ];

        return (IEvent)ctor.Invoke(args);
    }

    private static object Value(Type type, string name) =>
        type switch
        {
            _ when type == typeof(int) => 4312,
            _ when type == typeof(long) => 4312L,
            _ when type == typeof(bool) => false,
            _ when type == typeof(string) => $"fixture-{name}",
            _ when type == typeof(DateTime) => new DateTime(2026, 9, 6, 12, 0, 0, DateTimeKind.Utc),
            _ when type == typeof(PlayerId) => (PlayerId)4312,
            _ when type == typeof(PlayerId?) => (PlayerId)4312,
            _ when type == typeof(ImmutableArray<string>) => ImmutableArray.Create("FIXTURE_BADGE"),
            _ when type == typeof(IReadOnlyList<int>) => new List<int> { 4312 },
            _ when Nullable.GetUnderlyingType(type) is Type inner => Value(inner, name),
            // An enum's first declared value, not default(T): several of these enums have a
            // "none"/"unknown" member at zero, and a translator branching on it would be fed the
            // one input that makes it do nothing.
            _ when type.IsEnum => System.Enum.GetValues(type).GetValue(0)!,
            _ => throw new NotSupportedException(
                $"The event fixture does not know how to build a {type.Name} (parameter '{name}'). "
                    + "Add it rather than defaulting it: a defaulted field is a translator that "
                    + "looks tested and is not."
            ),
        };

    /// <summary>An id in the form a signal carries it, for asserting a target.</summary>
    public static string Id(long value) => value.ToString(CultureInfo.InvariantCulture);
}
