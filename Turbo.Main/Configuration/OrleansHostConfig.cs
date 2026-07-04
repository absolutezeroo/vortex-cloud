namespace Turbo.Main.Configuration;

/// <summary>
///     Silo networking endpoint, bound from <c>Turbo:Orleans</c>. Defaults match the previous
///     hardcoded single-node localhost setup, so binding this changes nothing until overridden.
/// </summary>
public sealed class OrleansHostConfig
{
    public const string SECTION_NAME = "Turbo:Orleans";

    public string AdvertisedIp { get; init; } = "127.0.0.1";
    public int SiloPort { get; init; } = 11111;
    public int GatewayPort { get; init; } = 3000;
}
