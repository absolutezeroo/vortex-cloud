using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Vortex.Runtime.AssemblyProcessing;

public static class AssemblyMemoryLoader
{
    public static LoadedAssembly LoadFromBytes(string mainDllPath)
    {
        if (!File.Exists(mainDllPath))
        {
            throw new FileNotFoundException("Plugin entry DLL not found.", mainDllPath);
        }

        string baseDir = Path.GetDirectoryName(mainDllPath)!;
        string entryName = Path.GetFileNameWithoutExtension(mainDllPath);
        Dictionary<string, (byte[] asm, byte[]? pdb)> managed = new Dictionary<
            string,
            (byte[] asm, byte[]? pdb)
        >(StringComparer.OrdinalIgnoreCase);
        List<string> shadowed = [];

        static byte[] ReadAll(string path) => File.ReadAllBytes(path);

        foreach (string dll in Directory.EnumerateFiles(baseDir, "*.dll"))
        {
            string name = Path.GetFileNameWithoutExtension(dll);

            if (managed.ContainsKey(name))
            {
                continue;
            }

            // A contract assembly copied into the plugin folder used to be byte-loaded here, giving
            // the plugin its own IVortexPlugin, its own IEventHandler<> and its own event records —
            // none of which the host would ever match, and nothing said so. Packaging was expected
            // to avoid it and nothing enforced it. Now it resolves from the default context like
            // every other shared type, so a mis-packaged plugin works instead of failing silently.
            if (
                !string.Equals(name, entryName, StringComparison.OrdinalIgnoreCase)
                && ByteLoadingAlc.IsHostContractAssembly(name)
            )
            {
                shadowed.Add(name);

                continue;
            }

            byte[] asmBytes = ReadAll(dll);
            string pdbPath = Path.ChangeExtension(dll, ".pdb");
            byte[]? pdbBytes = File.Exists(pdbPath) ? ReadAll(pdbPath) : null;

            managed[name] = (asmBytes, pdbBytes);
        }

        ByteLoadingAlc alc = new ByteLoadingAlc(baseDir, managed, entryName);
        Assembly asm = alc.LoadFromAssemblyName(new AssemblyName(entryName));

        return new LoadedAssembly(asm, alc, baseDir, shadowed);
    }

    public static async Task<bool> UnloadAndWaitAsync(
        ByteLoadingAlc alc,
        int maxMs = 5000,
        CancellationToken ct = default
    )
    {
        WeakReference wr = new WeakReference(alc);

        alc.Unload();

        Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

        while (sw.ElapsedMilliseconds < maxMs && !ct.IsCancellationRequested)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            if (!wr.IsAlive)
            {
                return true;
            }

            try
            {
                await Task.Delay(50, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        return !wr.IsAlive;
    }
}
