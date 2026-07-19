using System.Collections.Generic;

namespace Vortex.Primitives.Furniture.StuffData;

public interface INumberStuffData : IStuffData
{
    public List<int> Data { get; }
}
