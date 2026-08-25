using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Commerce;

namespace Vortex.Database.Tests.Commerce;

/// <summary>
/// A journal for the suites that are about something else. It records nothing and answers every step
/// as fresh, so a flow under test behaves exactly as it would on its first attempt — which is what a
/// test of the flow itself wants. Idempotence has its own suites.
/// </summary>
internal sealed class NullCommerceJournal : ICommerceJournal
{
    public Task OpenAsync(
        CommerceOperationId id,
        CommerceOperationKind kind,
        int playerId,
        string? detail,
        CancellationToken ct
    ) => Task.CompletedTask;

    public Task OpenIfNewAsync(
        CommerceOperationId id,
        CommerceOperationKind kind,
        int playerId,
        string? detail,
        CancellationToken ct
    ) => Task.CompletedTask;

    public Task TransitionAsync(
        CommerceOperationId id,
        CommerceOperationState state,
        string? step,
        string? error,
        CancellationToken ct
    ) => Task.CompletedTask;

    public Task<bool> TryRecordStepAsync(
        CommerceOperationId id,
        string stepKey,
        string? result,
        CancellationToken ct
    ) => Task.FromResult(true);

    public Task CompleteWithRelayAsync(
        CommerceOperationId id,
        Vortex.Primitives.Events.IEvent criticalEvent,
        CancellationToken ct
    ) => Task.CompletedTask;

    public Task<IReadOnlyList<CommerceRelayEntry>> GetUnrelayedAsync(
        int limit,
        CancellationToken ct
    ) => Task.FromResult<IReadOnlyList<CommerceRelayEntry>>([]);

    public Task MarkRelayedAsync(CommerceOperationId id, CancellationToken ct) =>
        Task.CompletedTask;

    public Task<string?> GetStepResultAsync(
        CommerceOperationId id,
        string stepKey,
        CancellationToken ct
    ) => Task.FromResult<string?>(null);

    public Task<IReadOnlyList<CommerceOperationRecord>> GetIncompletePivotedAsync(
        int limit,
        CancellationToken ct
    ) => Task.FromResult<IReadOnlyList<CommerceOperationRecord>>([]);
}
