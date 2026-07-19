using System.Collections.Generic;

namespace Vortex.Primitives.Furniture.StuffData;

public interface IStringStuffData : IStuffData
{
    public List<string> Data { get; }
}
