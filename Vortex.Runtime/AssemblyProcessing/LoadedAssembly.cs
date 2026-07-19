using System.Reflection;

namespace Vortex.Runtime.AssemblyProcessing;

public sealed record LoadedAssembly(Assembly Assembly, ByteLoadingAlc Alc, string BaseDir);
