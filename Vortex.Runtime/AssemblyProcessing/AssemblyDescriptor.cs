using System.Reflection;

namespace Vortex.Runtime.AssemblyProcessing;

public class AssemblyDescriptor
{
    public required string Key { get; init; }
    public required Assembly Assembly { get; init; }
    public ByteLoadingAlc? Alc { get; init; }
}
