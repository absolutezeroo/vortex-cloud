using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace Vortex.Runtime.AssemblyProcessing;

public static class AssemblyExplorer
{
    private static readonly ConditionalWeakTable<Assembly, Lazy<Type[]>> CONCRETE_TYPE_CACHE = [];

    public static Type? FindType(Assembly asm, Type type)
    {
        using AssemblyLoadContext.ContextualReflectionScope? _ = EnterContextual(asm);

        Type? candidate = null;

        foreach (TypeInfo ti in asm.DefinedTypes)
        {
            if (ti.IsAbstract || ti.IsInterface || ti.IsGenericTypeDefinition)
            {
                continue;
            }

            Type? asType = null;

            try
            {
                asType = ti.AsType();
            }
            catch
            {
                continue;
            }

            try
            {
                if (type.IsAssignableFrom(asType))
                {
                    if (candidate is null)
                    {
                        candidate = asType;
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"Multiple IVortexPlugin implementers in assembly {asm.GetName().Name}"
                        );
                    }
                }
            }
            catch
            {
                // ignore reflection oddities and keep scanning
            }
        }

        return candidate;
    }

    /// <summary>
    /// <paramref name="onNonPublicSkipped" /> is called for every type that implements the interface
    /// but is not public, and is therefore skipped. Such a type compiles and ships and is never
    /// registered, so the caller is expected to say so rather than let it disappear.
    /// </summary>
    public static IEnumerable<(
        Type Concrete,
        Type ClosedInterface,
        Type[] Args
    )> FindClosedImplementations(
        Assembly asm,
        Type openGenericInterface,
        Action<Type>? onNonPublicSkipped = null
    )
    {
        ArgumentNullException.ThrowIfNull(openGenericInterface);

        if (!openGenericInterface.IsGenericTypeDefinition)
        {
            throw new ArgumentException(
                "Must be an open generic, e.g. typeof(IFoo<>).",
                nameof(openGenericInterface)
            );
        }

        using AssemblyLoadContext.ContextualReflectionScope? _ = EnterContextual(asm);

        foreach (TypeInfo ti in asm.DefinedTypes)
        {
            if (ti.IsAbstract || ti.IsInterface || ti.IsGenericTypeDefinition)
            {
                continue;
            }

            Type concrete;

            try
            {
                concrete = ti.AsType();
            }
            catch
            {
                continue;
            }

            if (!ti.IsPublic)
            {
                if (
                    onNonPublicSkipped is not null
                    && ImplementsOpenGeneric(ti, openGenericInterface)
                )
                {
                    onNonPublicSkipped(concrete);
                }

                continue;
            }

            IEnumerable<Type> ifaces;

            try
            {
                ifaces = ti.ImplementedInterfaces;
            }
            catch
            {
                continue;
            }

            foreach (Type iface in ifaces)
            {
                bool match = false;
                Type[]? args = null;

                try
                {
                    if (
                        iface.IsGenericType
                        && ReferenceEquals(iface.GetGenericTypeDefinition(), openGenericInterface)
                    )
                    {
                        args = iface.GetGenericArguments();
                        match = true;
                    }
                }
                catch
                {
                    // ignore malformed interface
                }

                if (match && args is not null)
                {
                    yield return (concrete, iface, args);
                }
            }
        }
    }

    /// <summary>Whether the type closes <paramref name="openGenericInterface" />, tolerating the
    /// reflection failures the scan loops already swallow.</summary>
    private static bool ImplementsOpenGeneric(TypeInfo ti, Type openGenericInterface)
    {
        try
        {
            foreach (Type iface in ti.ImplementedInterfaces)
            {
                if (
                    iface.IsGenericType
                    && ReferenceEquals(iface.GetGenericTypeDefinition(), openGenericInterface)
                )
                {
                    return true;
                }
            }
        }
        catch
        {
            // A type whose interfaces cannot be read is not worth reporting on.
        }

        return false;
    }

    /// <summary>
    /// <paramref name="onNonPublicSkipped" /> is called for every non-public class assignable to
    /// <paramref name="targetType" />, for the same reason as in
    /// <see cref="FindClosedImplementations" />: it ships and is never registered.
    /// </summary>
    public static IEnumerable<Type> FindAssignees(
        Assembly asm,
        Type targetType,
        Action<Type>? onNonPublicSkipped = null
    )
    {
        ArgumentNullException.ThrowIfNull(targetType);

        using AssemblyLoadContext.ContextualReflectionScope? _ = EnterContextual(asm);

        foreach (TypeInfo ti in asm.DefinedTypes)
        {
            if (ti.IsAbstract || ti.IsInterface || ti.IsGenericTypeDefinition || !ti.IsClass)
            {
                continue;
            }

            Type concrete;

            try
            {
                concrete = ti.AsType();
            }
            catch
            {
                continue;
            }

            if (!targetType.IsAssignableFrom(concrete))
            {
                continue;
            }

            if (!ti.IsPublic)
            {
                onNonPublicSkipped?.Invoke(concrete);

                continue;
            }

            yield return concrete;
        }
    }

    public static MethodInfo ResolveImplementation(
        Type concrete,
        Type closedIface,
        string ifaceMethodName
    )
    {
        MethodInfo ifaceMethod =
            closedIface.GetMethod(ifaceMethodName)
            ?? throw new MissingMethodException(closedIface.FullName, ifaceMethodName);

        InterfaceMapping map = concrete.GetInterfaceMap(closedIface);

        for (int i = 0; i < map.InterfaceMethods.Length; i++)
        {
            if (map.InterfaceMethods[i] == ifaceMethod)
            {
                return map.TargetMethods[i];
            }
        }

        MethodInfo m =
            concrete.GetMethod(
                ifaceMethodName,
                ifaceMethod.GetParameters().Select(p => p.ParameterType).ToArray()
            ) ?? throw new MissingMethodException(concrete.FullName, ifaceMethodName);

        return m;
    }

    private static AssemblyLoadContext.ContextualReflectionScope? EnterContextual(Assembly asm)
    {
        AssemblyLoadContext? alc = AssemblyLoadContext.GetLoadContext(asm);

        return alc?.EnterContextualReflection();
    }
}
