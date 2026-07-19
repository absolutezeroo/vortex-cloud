using System;
using Microsoft.Extensions.DependencyInjection;

namespace Vortex.Runtime;

public static class ActivatorHelpers
{
    public static Func<IServiceProvider, object> BuildActivator(Type concrete)
    {
        ObjectFactory factory = ActivatorUtilities.CreateFactory(concrete, Type.EmptyTypes);
        object Activator(IServiceProvider sp) => factory(sp, null);
        return Activator;
    }
}
