using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Vortex.Database.Tests.Support;

/// <summary>A single intercepted call: the method that was invoked and the arguments it got.</summary>
public sealed record ProxyCall(MethodInfo Method, object?[]? Args);

/// <summary>
/// Interface stub for the wide grain interfaces. Hand-writing a fake for something like
/// <c>IPlayerGrain</c> means thirty members of which a test cares about two, so the handler answers
/// the calls it recognises and everything else falls through to a harmless default for its return
/// type (a completed task, a zeroed struct, null).
/// </summary>
// Not sealed: DispatchProxy generates a subclass at runtime.
public class FakeProxy : DispatchProxy
{
    private Func<ProxyCall, object?> _handler = _ => null;

    public static T Create<T>(Func<ProxyCall, object?> handler)
        where T : class
    {
        T proxy = Create<T, FakeProxy>();

        ((FakeProxy)(object)proxy)._handler = handler;

        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod is null)
        {
            return null;
        }

        // A null answer means "not one of the calls this test cares about", not "the result is
        // null" -- tests that need a null result never reach here because nothing asserts on it.
        return _handler(new ProxyCall(targetMethod, args))
            ?? DefaultValueFor(targetMethod.ReturnType);
    }

    private static object? DefaultValueFor(Type returnType)
    {
        if (returnType == typeof(void))
        {
            return null;
        }

        if (returnType == typeof(Task))
        {
            return Task.CompletedTask;
        }

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            Type resultType = returnType.GetGenericArguments()[0];

            return typeof(Task)
                .GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(resultType)
                .Invoke(null, [GetDefault(resultType)]);
        }

        return GetDefault(returnType);
    }

    private static object? GetDefault(Type type) =>
        type.IsValueType && Nullable.GetUnderlyingType(type) is null
            ? Activator.CreateInstance(type)
            : null;
}
