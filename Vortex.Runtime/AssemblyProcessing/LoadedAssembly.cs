using System.Collections.Generic;
using System.Reflection;

namespace Vortex.Runtime.AssemblyProcessing;

/// <summary>
/// <paramref name="ShadowedContractAssemblies" /> names the host contract DLLs that were found in
/// the plugin folder and deliberately not loaded from it. Empty for a correctly packaged plugin;
/// anything in it is worth telling the author about, because those files do nothing.
/// </summary>
public sealed record LoadedAssembly(
    Assembly Assembly,
    ByteLoadingAlc Alc,
    string BaseDir,
    IReadOnlyList<string> ShadowedContractAssemblies
);
