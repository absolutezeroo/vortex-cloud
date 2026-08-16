using System;
using System.Collections.Generic;
using System.Threading;

namespace Vortex.Database.Auditing;

/// <summary>
/// One row as it was, and as it became.
/// </summary>
/// <param name="Entity">The CLR entity name, e.g. <c>CatalogOfferEntity</c>.</param>
/// <param name="Table">The mapped table, e.g. <c>catalog_offers</c> — what an operator recognises.</param>
/// <param name="Id">Primary key, when the entity has a single-column one.</param>
/// <param name="Operation"><c>update</c> or <c>delete</c>.</param>
/// <param name="Before">
/// Every column for a delete; only the columns that actually changed for an update. Read from EF's
/// original values, so it is what the database held, not what a screen believed.
/// </param>
/// <param name="After">The new values of those same columns. Empty for a delete.</param>
public sealed record EntityChange(
    string Entity,
    string Table,
    string? Id,
    string Operation,
    IReadOnlyDictionary<string, string?> Before,
    IReadOnlyDictionary<string, string?> After
);

/// <summary>
/// Collects the before/after of every tracked row written inside a dashboard operation, so the audit
/// can record what a write actually did rather than only which id it was aimed at.
///
/// <para>
/// Opt-in by design. <see cref="Begin"/> arms the collector for the current async flow and nothing
/// else; outside a capture the interceptor returns on its first line. The game's own write path —
/// furniture moves, chat logs, wallet updates, every room tick — must not pay for this, and must not
/// flood the audit with rows nobody asked about.
/// </para>
///
/// <para>
/// Deliberately an <see cref="AsyncLocal{T}"/> static rather than a DI service: the interceptor is
/// constructed once with the context factory options, while the capture belongs to one operation's
/// async flow. Threading a scoped service through EF's interceptor pipeline would be the same
/// AsyncLocal with more ceremony.
/// </para>
/// </summary>
public static class EntityChangeCapture
{
    private static readonly AsyncLocal<Session?> _current = new();

    internal static Session? Current => _current.Value;

    /// <summary>Arms collection until the returned handle is disposed. Nested calls reuse the outer
    /// session, so a service that saves twice still reports one list.</summary>
    public static IEntityChangeCapture Begin()
    {
        if (_current.Value is { } existing)
        {
            return new NestedHandle(existing);
        }

        Session session = new();
        _current.Value = session;

        return session;
    }

    internal sealed class Session : IEntityChangeCapture
    {
        private readonly List<EntityChange> _changes = [];

        public IReadOnlyList<EntityChange> Changes => _changes;

        internal void Add(EntityChange change) => _changes.Add(change);

        public void Dispose() => _current.Value = null;
    }

    private sealed class NestedHandle(Session session) : IEntityChangeCapture
    {
        public IReadOnlyList<EntityChange> Changes => session.Changes;

        // The outer handle owns the session; releasing it here would blind the rest of the operation.
        public void Dispose() { }
    }
}

/// <summary>Handle returned by <see cref="EntityChangeCapture.Begin"/>.</summary>
public interface IEntityChangeCapture : IDisposable
{
    /// <summary>What was written so far, in the order EF saw it.</summary>
    IReadOnlyList<EntityChange> Changes { get; }
}
