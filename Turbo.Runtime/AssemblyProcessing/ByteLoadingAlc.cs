using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Turbo.Runtime.AssemblyProcessing;

public sealed class ByteLoadingAlc(
    string basePath,
    IReadOnlyDictionary<string, (byte[] asm, byte[]? pdb)> managed
) : AssemblyLoadContext(isCollectible: true)
{
    private readonly IReadOnlyDictionary<string, (byte[] asm, byte[]? pdb)> _managed = managed;
    private readonly AssemblyDependencyResolver _resolver = new(basePath);

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        string simple = assemblyName.Name!;

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
