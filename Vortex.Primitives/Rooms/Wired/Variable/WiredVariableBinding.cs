using Orleans;
using Vortex.Primitives.Rooms.Enums.Wired;

namespace Vortex.Primitives.Rooms.Wired.Variable;

[GenerateSerializer, Immutable]
public readonly record struct WiredVariableBinding(WiredVariableTargetType TargetType, int TargetId)
{
    public override string ToString() => $"{(int)TargetType}:{TargetId}";
}
