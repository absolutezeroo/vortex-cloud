using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Events.Registry;
using Vortex.Pipeline;
using Vortex.Pipeline.Delegates;
using Vortex.Primitives.Events;
using Vortex.Primitives.Plugins;
using Vortex.Primitives.Signals;
using Vortex.Runtime;
using Vortex.Runtime.AssemblyProcessing;

namespace Vortex.Signals;

/// <summary>
/// Discovers translators in an assembly, registers each as the handler for its event, and files its
/// shapes in the vocabulary.
/// </summary>
/// <remarks>
/// <para>
/// Runs beside <c>EventFeatureProcessor</c> under the same <c>AssemblyProcessor</c> — so over the
/// host and over every plugin, and the batch it returns removes both the handler and the shapes when
/// a plugin is unloaded. There is no new infrastructure here: a translator becomes an ordinary
/// handler, discovered and isolated like any other.
/// </para>
/// <para>
/// The one thing it does differently is build the host <em>once</em> and register it as a constant
/// activator. Every other handler in the hotel is constructed and disposed per invocation; a
/// translator has no state and no dependency, and the hottest of them runs once per tile walked.
/// Building it here also keeps a plugin's own service provider out of the picture: what a processor
/// resolves at registration time from a plugin provider does not see host services, and that has
/// taken a subsystem down at startup before.
/// </para>
/// </remarks>
public sealed class SignalTranslatorFeatureProcessor(
    EventRegistry registry,
    EnvelopeInvokerFactory<EventContext> invokerFactory,
    SignalVocabulary vocabulary,
    IEventPublisher publisher,
    ISignalInterest interest,
    ISignalMetrics metrics
) : IAssemblyFeatureProcessor
{
    private static readonly Type OpenTranslator = typeof(ISignalTranslator<>);

    public Task<IDisposable> ProcessAsync(
        Assembly asm,
        IServiceProvider sp,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(asm);

        RefuseNonPublicTranslators(asm);

        string? pluginPrefix = PluginPrefixOf(sp);
        CompositeDisposable batch = new();

        foreach (
            (Type concrete, Type _, Type[] args) in AssemblyExplorer.FindClosedImplementations(
                asm,
                OpenTranslator
            )
        )
        {
            Type eventType = args[0];
            ImmutableArray<SignalShape> shapes = ReadShapes(concrete, eventType, pluginPrefix);
            object translator = Instantiate(concrete);

            object host = Activator.CreateInstance(
                typeof(SignalTranslatorHost<>).MakeGenericType(eventType),
                translator,
                shapes.Select(s => s.Action).Distinct(StringComparer.Ordinal).ToImmutableArray(),
                publisher,
                interest,
                metrics,
                concrete.Name
            )!;

            HandlerInvoker<EventContext> invoker = invokerFactory.CreateHandlerInvoker(
                host.GetType(),
                eventType
            );

            // CA2000: ownership transfers to `batch`, which is this method's returned IDisposable
            // and is disposed by the caller (AssemblyProcessor).
#pragma warning disable CA2000
            batch.Add(vocabulary.Register(shapes));
            batch.Add(registry.RegisterHandler(eventType, sp, _ => host, invoker));
#pragma warning restore CA2000
        }

        return Task.FromResult<IDisposable>(batch);
    }

    /// <summary>
    /// A non-public translator is a hard error, not a warning.
    /// </summary>
    /// <remarks>
    /// <c>AssemblyExplorer</c> keeps public types only and settles for a callback, which is right
    /// for one handler among thirty: the others still run. A missing translator is different — the
    /// action it produces goes entirely silent, no exception is ever raised, and the generic
    /// translator test discovers through this same path so it would not see the absence either. A
    /// silo that refuses to start names the type; a silent hotel names nothing.
    /// </remarks>
    private static void RefuseNonPublicTranslators(Assembly asm)
    {
        List<string> offenders = [];

        foreach (TypeInfo ti in asm.DefinedTypes)
        {
            if (ti.IsPublic || ti.IsAbstract || ti.IsInterface || ti.IsGenericTypeDefinition)
            {
                continue;
            }

            if (ti.ImplementedInterfaces.Any(IsTranslatorInterface))
            {
                offenders.Add(ti.FullName ?? ti.Name);
            }
        }

        if (offenders.Count > 0)
        {
            throw new InvalidOperationException(
                $"Signal translators must be public, or they are never discovered and their action "
                    + $"goes silent with no error: {string.Join(", ", offenders)}."
            );
        }
    }

    private static bool IsTranslatorInterface(Type t) =>
        t.IsGenericType && t.GetGenericTypeDefinition() == OpenTranslator;

    private static object Instantiate(Type concrete)
    {
        if (concrete.GetConstructor(Type.EmptyTypes) is null)
        {
            throw new InvalidOperationException(
                $"{concrete.FullName} is a signal translator, so it must have a parameterless "
                    + "constructor: a translation is a pure function, and anything it would need "
                    + "injected belongs on the event instead."
            );
        }

        return Activator.CreateInstance(concrete)!;
    }

    /// <summary>Reads the static <c>Shapes</c> the interface makes mandatory, and validates them.</summary>
    private static ImmutableArray<SignalShape> ReadShapes(
        Type concrete,
        Type eventType,
        string? pluginPrefix
    )
    {
        PropertyInfo property =
            OpenTranslator
                .MakeGenericType(eventType)
                .GetProperty(nameof(ISignalTranslator<IEvent>.Shapes))
            ?? throw new InvalidOperationException("ISignalTranslator<>.Shapes is missing.");

        InterfaceMapping map = concrete.GetInterfaceMap(OpenTranslator.MakeGenericType(eventType));
        MethodInfo getter = map.TargetMethods[
            Array.IndexOf(map.InterfaceMethods, property.GetGetMethod()!)
        ];

        ImmutableArray<SignalShape> shapes =
            (ImmutableArray<SignalShape>)getter.Invoke(null, null)!;

        if (shapes.IsDefaultOrEmpty)
        {
            throw new InvalidOperationException(
                $"{concrete.FullName} declares no shape, so nothing it produces could ever be "
                    + "filtered or offered in the editor."
            );
        }

        foreach (SignalShape shape in shapes)
        {
            Validate(concrete, shape, pluginPrefix);
        }

        return shapes;
    }

    private static void Validate(Type concrete, SignalShape shape, string? pluginPrefix)
    {
        RequirePrefix(concrete, shape.Action, pluginPrefix, "action");

        foreach (FactKey fact in shape.Facts)
        {
            RequirePrefix(concrete, fact.Key, pluginPrefix, "fact");

            // A closed fact with nothing to choose from is a select with no options: the editor
            // would render an empty dropdown and the validator would refuse every value.
            if (fact.Kind == FactKind.Enum && fact.EnumValues.IsDefaultOrEmpty)
            {
                throw new InvalidOperationException(
                    $"{concrete.FullName} declares fact '{fact.Key}' as Enum with no values."
                );
            }
        }
    }

    /// <summary>
    /// A plugin's own keys must be qualified with its plugin key; core keys may be reused as they
    /// are.
    /// </summary>
    /// <remarks>
    /// Validated, never rewritten. Rewriting at load would put <c>acme:trophy</c> in the vocabulary
    /// while the host published the raw <c>trophy</c> that <c>Translate</c> returned, and no filter
    /// would ever match. Validating also lets a plugin say "in that room" with the core's own
    /// <c>room</c> fact instead of an accidental <c>acme:room</c> nobody else understands.
    /// </remarks>
    private static void RequirePrefix(Type concrete, string key, string? pluginPrefix, string what)
    {
        if (pluginPrefix is null || key.StartsWith($"{pluginPrefix}:", StringComparison.Ordinal))
        {
            return;
        }

        bool isCoreKey =
            Facts.All.Any(f => string.Equals(f.Key, key, StringComparison.Ordinal))
            || string.Equals(key, Facts.TargetKey, StringComparison.Ordinal)
            || SignalActions.All.Contains(key);

        if (!isCoreKey)
        {
            throw new InvalidOperationException(
                $"{concrete.FullName} declares {what} '{key}', which is neither a core {what} nor "
                    + $"prefixed with '{pluginPrefix}:'."
            );
        }
    }

    private static string? PluginPrefixOf(IServiceProvider sp) =>
        (sp.GetService(typeof(PluginManifest)) as PluginManifest)?.Key;
}
