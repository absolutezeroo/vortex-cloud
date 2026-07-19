using System.Text;

namespace Vortex.Primitives.Packets;

public class VortexPacket(int header) : IVortexPacket
{
    protected readonly StringBuilder _logger = new();

    public int Header { get; set; } = header;

    public override string ToString()
    {
        return _logger.ToString();
    }
}
