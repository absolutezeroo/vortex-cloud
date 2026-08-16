using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Vortex.Database.Auditing;

/// <summary>
/// Records the before/after of every tracked update and delete that happens inside an armed
/// <see cref="EntityChangeCapture"/>, reading EF's own original values just before they are written
/// away.
///
/// <para>
/// This is the only place the "what did it used to be?" question can be answered honestly. The
/// dashboard's own diff is computed in the browser from the row on screen; this one comes from the
/// change tracker, which loaded it from the database. When they disagree, this is the one that is
/// right.
/// </para>
///
/// <para>
/// Not covered, on purpose: <c>ExecuteUpdateAsync</c>/<c>ExecuteDeleteAsync</c> bypass the change
/// tracker entirely, so a bulk statement leaves no snapshot here. Nothing silently pretends
/// otherwise — those operations simply record no entity change, and the audit still carries the
/// action and its request payload.
/// </para>
/// </summary>
public sealed class EntityChangeInterceptor : SaveChangesInterceptor
{
    /// <summary>
    /// Columns whose value must never reach an audit record, matched case-insensitively on the
    /// property name. An audit trail is read by more people than the table it describes.
    /// </summary>
    private static readonly HashSet<string> _redacted = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password",
        "PasswordHash",
        "AuthTicket",
        "SsoTicket",
        "Ticket",
        "Token",
        "RefreshToken",
        "Secret",
        "ApiKey",
        "IpAddress",
        "IpHash",
        "LastIp",
        "Email",
        "MachineId",
    };

    private const string Redacted = "***";

    /// <summary>Long text (a wired blob, a forum post) would drown the record; the fact that it
    /// changed is the part worth keeping.</summary>
    private const int MaxValueLength = 512;

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result
    )
    {
        Collect(eventData);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        Collect(eventData);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void Collect(DbContextEventData eventData)
    {
        if (EntityChangeCapture.Current is not { } session || eventData.Context is null)
        {
            return;
        }

        foreach (EntityEntry entry in eventData.Context.ChangeTracker.Entries())
        {
            EntityChange? change = entry.State switch
            {
                EntityState.Deleted => Describe(entry, "delete"),
                EntityState.Modified => Describe(entry, "update"),
                _ => null,
            };

            if (change is not null)
            {
                session.Add(change);
            }
        }
    }

    private static EntityChange? Describe(EntityEntry entry, string operation)
    {
        bool isDelete = operation == "delete";

        // On an update only the touched columns are interesting; on a delete the whole row is the
        // thing being lost, so all of it is kept.
        List<PropertyEntry> properties =
        [
            .. entry
                .Properties.Where(p => isDelete || p.IsModified)
                .Where(p => !p.Metadata.IsShadowProperty()),
        ];

        if (properties.Count == 0)
        {
            return null;
        }

        Dictionary<string, string?> before = [];
        Dictionary<string, string?> after = [];

        foreach (PropertyEntry property in properties)
        {
            string name = property.Metadata.Name;

            before[name] = Format(name, property.OriginalValue);

            if (!isDelete)
            {
                after[name] = Format(name, property.CurrentValue);
            }
        }

        return new EntityChange(
            entry.Metadata.ClrType.Name,
            entry.Metadata.GetTableName() ?? entry.Metadata.ClrType.Name,
            ReadKey(entry),
            operation,
            before,
            after
        );
    }

    private static string? ReadKey(EntityEntry entry)
    {
        IKey? key = entry.Metadata.FindPrimaryKey();

        if (key is null || key.Properties.Count != 1)
        {
            return null;
        }

        object? value = entry.Property(key.Properties[0].Name).CurrentValue;

        return value?.ToString();
    }

    private static string? Format(string propertyName, object? value)
    {
        if (_redacted.Contains(propertyName))
        {
            return Redacted;
        }

        if (value is null)
        {
            return null;
        }

        string text = value switch
        {
            DateTime dt => dt.ToString("O", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };

        return text.Length > MaxValueLength ? text[..MaxValueLength] + "…" : text;
    }
}
