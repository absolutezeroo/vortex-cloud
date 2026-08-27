using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Vortex.Runtime.AssemblyProcessing;

public sealed class ByteLoadingAlc(
    string basePath,
    IReadOnlyDictionary<string, (byte[] asm, byte[]? pdb)> managed,
    string entryAssemblyName
) : AssemblyLoadContext(isCollectible: true)
{
    private readonly IReadOnlyDictionary<string, (byte[] asm, byte[]? pdb)> _managed = managed;
    private readonly AssemblyDependencyResolver _resolver = new(basePath);
    private readonly string _entryAssemblyName = entryAssemblyName;

    /// <summary>
    /// True for an assembly whose types the host and a plugin must agree on — <c>IVortexPlugin</c>,
    /// <c>IEventHandler&lt;&gt;</c>, every event record. Loading a second copy of one into this
    /// context produces types that are structurally identical and never equal, so a handler
    /// registers against an event the publisher does not raise and no error is ever reported.
    /// <para>
    /// It is not enough for the name to look like ours: a plugin split over
    /// <c>Vortex.MyPlugin.Core</c> and <c>Vortex.MyPlugin</c> must still load its own halves. What
    /// makes an assembly shared is that the host already has it, so that is what is asked.
    /// </para>
    /// </summary>
    public static bool IsHostContractAssembly(string simpleName)
    {
        if (!simpleName.StartsWith("Vortex.", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return HOST_CONTRACT_CACHE.GetOrAdd(
            simpleName,
            static name =>
            {
                try
                {
                    return Default.LoadFromAssemblyName(new AssemblyName(name)) is not null;
                }
                catch (Exception ex)
                    when (ex
                            is FileNotFoundException
                                or FileLoadException
                                or BadImageFormatException
                    )
                {
                    return false;
                }
            }
        );
    }

    private static readonly ConcurrentDictionary<string, bool> HOST_CONTRACT_CACHE = new(
        StringComparer.OrdinalIgnoreCase
    );

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        string simple = assemblyName.Name!;

        // Returning null delegates to the default context, which is where the host's own copy is.
        if (
            !string.Equals(simple, _entryAssemblyName, StringComparison.OrdinalIgnoreCase)
            && IsHostContractAssembly(simple)
        )
        {
            return null;
        }

        if (_managed.TryGetValue(simple, out (byte[] asm, byte[]? pdb) blob))
        {
            using MemoryStream msAsm = new MemoryStream(blob.asm, writable: false);

            if (blob.pdb is { } pdb)
            {
                using MemoryStream msPdb = new MemoryStream(pdb, writable: false);

                return LoadFromStream(msAsm, msPdb);
            }

            return LoadFromStream(msAsm);
        }

        string? path = _resolver.ResolveAssemblyToPath(assemblyName);
        if (path is null)
        {
            return null;
        }

        if (Path.GetExtension(path).Equals(".dll", StringComparison.OrdinalIgnoreCase))
        {
            string pdbPath = Path.ChangeExtension(path, ".pdb");
            byte[] asmBytes = File.ReadAllBytes(path);
            byte[]? pdbBytes = File.Exists(pdbPath) ? File.ReadAllBytes(pdbPath) : null;

            using MemoryStream msAsm = new MemoryStream(asmBytes, writable: false);

            if (pdbBytes is { })
            {
                using MemoryStream msPdb = new MemoryStream(pdbBytes, writable: false);
                return LoadFromStream(msAsm, msPdb);
            }

            return LoadFromStream(msAsm);
        }

        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        string? path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);

        if (path is null)
        {
            return IntPtr.Zero;
        }

        return LoadUnmanagedDllFromPath(path);
    }
}
