using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Turbo.Events.Registry;
using Turbo.Observability.Events;
using Turbo.Primitives.Events;
using Turbo.Primitives.Observability;
using Xunit;

namespace Turbo.Rooms.Tests.Observability;

public sealed class EconomyLedgerTests
{
    [Fact]
    public async Task DebitMovement_RecordsDebitLedgerEntry()
    {
        RecordingEconomyLedger ledger = new RecordingEconomyLedger();
        CurrencyChangedLedgerHandler handler = new CurrencyChangedLedgerHandler(ledger);

        await handler.HandleAsync(
            new CurrencyChangedEvent(7, "Credits", null, -25, 75),
            Context(),
            CancellationToken.None
        );

        EconomyLedgerEvent entry = ledger.Entries.Should().ContainSingle().Which;
        entry.PlayerId.Should().Be(7);
        entry.Currency.Should().Be("Credits");
        entry.ActivityPointType.Should().BeNull();
        entry.Delta.Should().Be(-25);
        entry.BalanceAfter.Should().Be(75);
        entry.Reason.Should().Be(EconomyReason.Debit);
    }

    [Fact]
    public async Task CreditMovement_RecordsGrantLedgerEntry()
    {
        RecordingEconomyLedger ledger = new RecordingEconomyLedger();
        CurrencyChangedLedgerHandler handler = new CurrencyChangedLedgerHandler(ledger);

        await handler.HandleAsync(
            new CurrencyChangedEvent(8, "ActivityPoints", 5, 40, 140),
            Context(),
            CancellationToken.None
        );

        EconomyLedgerEvent entry = ledger.Entries.Should().ContainSingle().Which;
        entry.PlayerId.Should().Be(8);
        entry.Currency.Should().Be("ActivityPoints");
        entry.ActivityPointType.Should().Be(5);
        entry.Delta.Should().Be(40);
        entry.BalanceAfter.Should().Be(140);
        entry.Reason.Should().Be(EconomyReason.Grant);
    }

    [Fact]
    public async Task ZeroDeltaMovement_RecordsNoLedgerEntry()
    {
        RecordingEconomyLedger ledger = new RecordingEconomyLedger();
        CurrencyChangedLedgerHandler handler = new CurrencyChangedLedgerHandler(ledger);

        await handler.HandleAsync(
            new CurrencyChangedEvent(9, "Credits", null, 0, 75),
            Context(),
            CancellationToken.None
        );

        ledger.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task EveryPublishedMovement_RecordsExactlyOneLedgerEntry()
    {
        RecordingEconomyLedger ledger = new RecordingEconomyLedger();
        CurrencyChangedLedgerHandler handler = new CurrencyChangedLedgerHandler(ledger);
        List<CurrencyChangedEvent> movements = new List<CurrencyChangedEvent>
        {
            new CurrencyChangedEvent(10, "Credits", null, -15, 185),
            new CurrencyChangedEvent(10, "Credits", null, 20, 205),
            new CurrencyChangedEvent(10, "ActivityPoints", 1, -5, 95),
        };

        foreach (CurrencyChangedEvent movement in movements)
        {
            await handler.HandleAsync(movement, Context(), CancellationToken.None);
        }

        ledger.Entries.Should().HaveCount(movements.Count);
        ledger.Entries.Should().OnlyContain(e => e.Delta != 0);
        ledger.Entries.Should().OnlyContain(e => e.Reason == ExpectedReason(e.Delta));
    }

    [Fact]
    public async Task SameMovement_MapsToSameLedgerEntryEachTime()
    {
        RecordingEconomyLedger ledger = new RecordingEconomyLedger();
        CurrencyChangedLedgerHandler handler = new CurrencyChangedLedgerHandler(ledger);
        CurrencyChangedEvent movement = new CurrencyChangedEvent(11, "Credits", null, -30, 70);

        await handler.HandleAsync(movement, Context(), CancellationToken.None);
        await handler.HandleAsync(movement, Context(), CancellationToken.None);

        ledger.Entries.Should().HaveCount(2);
        ledger.Entries[1].Should().BeEquivalentTo(ledger.Entries[0]);
    }

    private static EconomyReason ExpectedReason(long delta)
    {
        if (delta < 0)
        {
            return EconomyReason.Debit;
        }

        return EconomyReason.Grant;
    }

    private static EventContext Context()
    {
        return new EventContext { CorrelationId = string.Empty };
    }

    private sealed class RecordingEconomyLedger : IEconomyLedger
    {
        private readonly List<EconomyLedgerEvent> _entries = new List<EconomyLedgerEvent>();

        public IReadOnlyList<EconomyLedgerEvent> Entries => _entries;

        public void Record(in EconomyLedgerEvent entry)
        {
            _entries.Add(entry);
        }
    }
}
