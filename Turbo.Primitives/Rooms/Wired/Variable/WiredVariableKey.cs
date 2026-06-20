using System;
using Turbo.Primitives.Rooms.Enums.Wired;

namespace Turbo.Primitives.Rooms.Wired.Variable;

public readonly record struct WiredVariableKey(
    WiredVariableId VariableId,
    WiredVariableTargetType TargetType,
    int TargetId
)
{
    public string ToStorageKey() => $"{VariableId}|{(int)TargetType}|{TargetId}";

    public static WiredVariableKey FromStorageKey(string storageKey)
    {
        string[] parts = storageKey.Split('|');

        if (
            parts.Length != 3
            || !ulong.TryParse(parts[0], out ulong variableId)
            || !int.TryParse(parts[1], out int targetType)
            || !int.TryParse(parts[2], out int targetId)
        )
        {
            throw new FormatException($"Invalid WiredVariableKey storage key: {storageKey}");
        }

        return new WiredVariableKey(
            WiredVariableId.Parse(variableId.ToString()),
            (WiredVariableTargetType)targetType,
            targetId
        );
    }
}
