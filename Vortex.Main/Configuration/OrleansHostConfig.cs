namespace Vortex.Main.Configuration;

/// <summary>
///     Silo networking endpoint, bound from <c>Vortex:Orleans</c>. Defaults match the previous
///     hardcoded single-node localhost setup, so binding this changes nothing until overridden.
/// </summary>
public sealed class OrleansHostConfig
{
    public const string SECTION_NAME = "Vortex:Orleans";

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

    /// <summary>
    ///     "localhost" (default, unchanged) or "adonet" for multi-silo clustering backed by the same
    ///     MySQL database as <c>Vortex:Database:ConnectionString</c>. Selecting "adonet" requires
    ///     Orleans's official clustering SQL scripts (https://aka.ms/orleans-sql-scripts) to already
    ///     be applied to that database — this is a deployment prerequisite this process cannot apply
    ///     for you, the same way EF migrations must already be applied before startup.
    /// </summary>
    public string ClusteringProvider { get; init; } = "localhost";

    /// <summary>
    ///     "memory" (default, unchanged) or "adonet" to persist PubSubStore (the only grain storage
    ///     actually used — see ORL-01) so in-flight stream messages survive a silo restart. Same SQL
    ///     script prerequisite as <see cref="ClusteringProvider"/>.
    /// </summary>
    public string GrainStorageProvider { get; init; } = "memory";

    /// <summary>
    ///     ADO.NET provider invariant name for "adonet" mode. Defaults to the driver this project
    ///     already ships (MySqlConnector, via Pomelo.EntityFrameworkCore.MySql); override only if
    ///     pointing Orleans at a different ADO.NET provider/database engine.
    /// </summary>
    public string Invariant { get; init; } = "MySqlConnector";
}
