using System;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Primitives.Rooms.Providers;

public interface IRoomObjectLogicProvider
{
    public IDisposable RegisterLogic(
        string logicType,
        IServiceProvider sp,
        Func<IServiceProvider, IRoomObjectContext, IRoomObjectLogic> factory
    );
    public IRoomObjectLogic CreateLogicInstance(string logicType, IRoomObjectContext ctx);
}
