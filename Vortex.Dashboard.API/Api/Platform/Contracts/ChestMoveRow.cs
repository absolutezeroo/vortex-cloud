using System;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>
/// One withdrawal from or deposit into a wired chest.
/// </summary>
/// <remarks>
/// Part of the timeline because an investigation that sees a furni leave a room but not the chest
/// it went into stops one step short of the answer.
/// </remarks>
public sealed record ChestMoveRow(
    DateTime CreatedAt,
    int ChestId,
    int RoomId,
    string? RoomName,
    int TransactionType,
    string DefinitionInfo,
    int WithdrawFurniCount,
    int DepositFurniCount,
    int WithdrawCoinsCount,
    int DepositCoinsCount
);
