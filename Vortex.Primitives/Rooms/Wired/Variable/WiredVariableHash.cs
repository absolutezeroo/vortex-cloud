using Orleans;

namespace Vortex.Primitives.Rooms.Wired.Variable;

[GenerateSerializer, Immutable]
public readonly record struct WiredVariableHash(int Value);
