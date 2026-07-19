using System.Collections.Generic;

namespace Vortex.Primitives.Furniture.StuffData;

public interface IMapStuffData : IStuffData
{
    public Dictionary<string, string> Data { get; }
}
