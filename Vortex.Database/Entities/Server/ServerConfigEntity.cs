using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Server;

/// <summary>
/// Admin-editable server configuration as typed key/value pairs (the store behind
/// <c>IServerConfigGrain</c>). Home for tunable gameplay knobs an operator changes at runtime
/// (limits, thresholds, MOTD, toggles) — as opposed to bootstrap/secret settings, which stay in
/// appsettings. Values are stored as strings (JSON for structured values) and parsed by the grain's
/// typed accessors.
/// </summary>
[Table("server_config")]
[Index(nameof(Key), IsUnique = true)]
public class ServerConfigEntity : VortexEntity
{
    [Column("config_key")]
    [MaxLength(128)]
    public required string Key { get; set; }

    [Column("config_value")]
    public required string Value { get; set; }

    [Column("description")]
    [MaxLength(256)]
    public string? Description { get; set; }
}
