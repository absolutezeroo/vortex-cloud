using System.Collections.Immutable;
using Orleans;

namespace Vortex.Primitives.Rooms.Snapshots.Wired;

/// <summary>One movement in or out of a chest, as the log screens show it.</summary>
/// <remarks>
/// The player's name travels with the row rather than being resolved by the reader: a log says who
/// did something under the name they used at the time.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record WiredTransactionSnapshot
{
    [Id(0)]
    public required long TransactionId { get; init; }

    /// <summary>The room the chest stood in. The client calls it the flat id.</summary>
    [Id(1)]
    public required int RoomId { get; init; }

    /// <summary>0 manual, 1 wired, 2 contract payment, 3 contract reward, 4 contract trade,
    /// 5 auto-withdraw. The client localises the number.</summary>
    [Id(2)]
    public required int TransactionType { get; init; }

    /// <summary>What moved, in one line, for the details screen.</summary>
    [Id(3)]
    public required string DefinitionInfo { get; init; }

    [Id(4)]
    public required int PlayerId { get; init; }

    [Id(5)]
    public required string PlayerName { get; init; }

    /// <summary>Unix milliseconds. The client keeps it to sort by.</summary>
    [Id(6)]
    public required long Timestamp { get; init; }

    /// <summary>The same instant already formatted, because the client prints this one as-is.</summary>
    [Id(7)]
    public required string ReadableTimestamp { get; init; }

    [Id(8)]
    public required int ChestCount { get; init; }

    [Id(9)]
    public required int WithdrawFurniCount { get; init; }

    [Id(10)]
    public required int DepositFurniCount { get; init; }

    [Id(11)]
    public required int WithdrawCoinsCount { get; init; }

    [Id(12)]
    public required int DepositCoinsCount { get; init; }
}

/// <summary>A page of a chest's or a room's transaction log.</summary>
/// <remarks>
/// <see cref="LogListType"/> tells the client which list it is looking at — 0 a chest, 1 a room —
/// and <see cref="LogListId"/> is that chest or that room. The client re-requests later pages with
/// the id it reads back here, so answering with the wrong pair sends it asking about the wrong
/// thing.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record WiredTransactionsSnapshot
{
    [Id(0)]
    public required int LogListType { get; init; }

    [Id(1)]
    public required long LogListId { get; init; }

    [Id(2)]
    public required int TotalLogs { get; init; }

    [Id(3)]
    public required int CurrentPage { get; init; }

    /// <summary>The page size, echoed back. The chests tab compares it against the size it asked
    /// for to tell its own preview apart from a full listing.</summary>
    [Id(4)]
    public required int Amount { get; init; }

    [Id(5)]
    public required ImmutableArray<WiredTransactionSnapshot> Logs { get; init; }
}
