namespace Vortex.Networking.Configuration;

public class NetworkingConfig
{
    public const string SECTION_NAME = "Turbo:Networking";

    /// <summary>
    ///     Default cap on a single client-declared packet body length, in bytes. Bounds
    ///     memory/CPU exposure from a malformed or hostile frame header.
    /// </summary>
    public const int DefaultMaxPacketBodyBytes = 65536;

    public int PingIntervalMilliseconds { get; init; } = 10000;

    public int MaxPacketBodyBytes { get; init; } = DefaultMaxPacketBodyBytes;
}
