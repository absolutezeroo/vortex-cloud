using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Vortex.Hosting.Tests.Architecture;

/// <summary>
/// Every type this solution ships, loaded from the test output directory. The architecture guards
/// need to see the whole codebase — a <c>[Reentrant]</c> added in <c>Vortex.Rooms</c> is exactly the
/// kind of thing a test that only looked at its own references would miss — and every project's
/// output is copied here by the build.
/// </summary>
internal static class VortexTypes
{
    private static Type[]? _cached;

    public static IReadOnlyCollection<Type> All()
    {
        if (_cached is not null)
        {
            return _cached;
        }

        List<Type> types = [];

        foreach (string dll in Directory.EnumerateFiles(AppContext.BaseDirectory, "Vortex.*.dll"))
        {
            if (Path.GetFileNameWithoutExtension(dll).EndsWith(".Tests", StringComparison.Ordinal))
            {
                continue;
            }

            Assembly assembly;

            try
            {
                assembly = Assembly.LoadFrom(dll);
            }
            catch (Exception ex) when (ex is BadImageFormatException or FileLoadException)
            {
                continue;
            }

            try
            {
                types.AddRange(assembly.GetTypes());
            }
            catch (ReflectionTypeLoadException ex)
            {
                // A type whose dependency is missing from the test output tells us nothing about the
                // invariants below; the ones that did load still do.
                types.AddRange(ex.Types.Where(t => t is not null)!);
            }
        }

        _cached = [.. types];

        return _cached;
    }
}
