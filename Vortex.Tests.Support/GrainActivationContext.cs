using System;
using System.Reflection;
using Orleans;
using Orleans.Runtime;

namespace Vortex.Tests.Support;

/// <summary>
/// Builds a grain outside a silo.
///
/// Grains here read their own primary key, which Orleans answers from the activation context. A
/// grain picks that context up in its base constructor, from the ambient one the runtime sets around
/// activation -- so a grain built with <c>new</c> in a test has none, and anything reading its key
/// throws. Some grains only read the key inside methods, but others (<c>RoomGrain</c>) read it in
/// their own constructor, so patching the instance afterwards is too late.
///
/// Both cases are covered by setting that ambient context for the duration of construction, exactly
/// as the runtime does. The alternative is standing up a full <c>TestCluster</c> with the whole DI
/// graph, which is a lot of machinery for a test that wants to call one method and inspect the
/// resulting state.
/// </summary>
public static class GrainActivationContext
{
    private static readonly MethodInfo SetAmbientContext = FindRuntimeContextMethod(
        "SetExecutionContext"
    );

    private static readonly MethodInfo ResetAmbientContext = FindRuntimeContextMethod(
        "ResetExecutionContext"
    );

    /// <summary>Constructs <typeparamref name="TGrain"/> with <paramref name="primaryKey"/> already
    /// readable from inside its constructor.</summary>
    public static TGrain CreateWithIntegerKey<TGrain>(long primaryKey, params object?[] ctorArgs)
        where TGrain : Grain
    {
        IGrainContext context = CreateContext(typeof(TGrain), primaryKey);

        // SetExecutionContext(newContext, out existingContext): the second argument comes back
        // written, and ResetExecutionContext wants it so nesting restores rather than clears.
        object?[] setArgs = [context, null];
        SetAmbientContext.Invoke(null, setArgs);

        try
        {
            return (TGrain)Activator.CreateInstance(typeof(TGrain), ctorArgs)!;
        }
        finally
        {
            ResetAmbientContext.Invoke(null, [setArgs[1]]);
        }
    }

    /// <summary>An activation context whose only job is to answer with a grain id built from
    /// <paramref name="primaryKey"/>.</summary>
    public static IGrainContext CreateContext(Type grainType, long primaryKey)
    {
        GrainId grainId = GrainId.Create(
            GrainType.Create(grainType.Name.ToLowerInvariant()),
            GrainIdKeyExtensions.CreateIntegerKey(primaryKey)
        );

        // Grain's base constructor resolves its runtime out of ActivationServices. A stub runtime is
        // enough and is what lets a grain that arms a timer run at all -- the timer is never going
        // to fire here, but registering one must not throw.
        // Any interface the grain or the runtime resolves gets a stub. Orleans looks several of
        // these up unchecked -- the timer registry among them -- so handing back null turns into a
        // null dereference inside Orleans rather than a readable failure.
        IServiceProvider services = FakeProxy.Create<IServiceProvider>(call =>
            call.Args?[0] is Type { IsInterface: true } serviceType
                ? StubComponent(serviceType)
                : null
        );

        return FakeProxy.Create<IGrainContext>(call =>
            call.Method.Name switch
            {
                $"get_{nameof(IGrainContext.GrainId)}" => grainId,
                $"get_{nameof(IGrainContext.ActivationServices)}" => services,

                // Grains reach for activation components here -- the timer registry above all.
                // Every one gets a stub, so arming a timer succeeds and does nothing, which is what
                // a test wants from a timer it will never let fire.
                nameof(IGrainContext.GetComponent) => StubComponent(
                    call.Method.GetGenericArguments()[0]
                ),

                // Orleans has moved which member it reaches for across versions (registering a
                // timer has gone through more than one), so anything else handing back an interface
                // gets a stub too rather than a null the runtime will dereference.
                _ => call.Method.ReturnType.IsInterface
                    ? StubComponent(call.Method.ReturnType)
                    : null,
            }
        );
    }

    /// <summary>
    /// A stub activation component whose interface-returning members hand back further stubs.
    /// Registering a timer, for instance, goes registry -> timer and Orleans then touches the timer
    /// it got back, so a component that answers everything with null is not enough.
    /// </summary>
    private static object StubComponent(Type componentType) =>
        FakeProxy.CreateFor(
            componentType,
            call =>
                call.Method.ReturnType.IsInterface ? StubComponent(call.Method.ReturnType) : null
        );

    private static MethodInfo FindRuntimeContextMethod(string name)
    {
        Type runtimeContext =
            typeof(IGrainContext).Assembly.GetType("Orleans.Runtime.RuntimeContext")
            ?? throw new InvalidOperationException("Orleans.Runtime.RuntimeContext moved.");

        return runtimeContext.GetMethod(
                name,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
            ) ?? throw new InvalidOperationException($"RuntimeContext.{name} moved.");
    }
}
