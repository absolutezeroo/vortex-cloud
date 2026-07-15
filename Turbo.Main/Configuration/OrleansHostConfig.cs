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

    /// <summary>
    ///     Single-node localhost clustering with in-memory grain storage/streams loses all
    ///     <c>[PersistentState]</c> grain state on every process restart and cannot scale beyond one
    ///     silo. Outside the Development environment this is refused at startup unless explicitly
    ///     opted into here — set this only for a deliberate single-node, restart-tolerant-data-loss
    ///     deployment, and configure a persistent clustering/storage provider otherwise.
    /// </summary>
    public bool AllowUnclusteredOutsideDevelopment { get; init; }
}
