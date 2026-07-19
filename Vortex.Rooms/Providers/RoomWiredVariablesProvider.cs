using System;
using System.Collections.Generic;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Primitives.Rooms.Providers;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Wired.Variables;
using Vortex.Runtime;

namespace Vortex.Rooms.Providers;

public sealed class RoomWiredVariablesProvider(IServiceProvider host) : IRoomWiredVariablesProvider
{
    private readonly IServiceProvider _host = host;
    private readonly List<WiredVariableReg> _variables = [];

    public IDisposable RegisterVariable(
        IServiceProvider sp,
        Func<IServiceProvider, IRoomGrain, IWiredVariable> factory
    )
    {
        WiredVariableReg reg = new WiredVariableReg(sp, factory);

        _variables.Add(reg);

        return new ActionDisposable(() => _variables.Remove(reg));
    }

    public IEnumerable<IWiredVariable> BuildVariablesForRoom(IRoomGrain roomGrain)
    {
        foreach (WiredVariableReg reg in _variables)
        {
            IServiceProvider sp = reg.ServiceProvider;

            if (sp != _host)
            {
                sp = new CompositeServiceProvider(sp, _host);
            }

            yield return reg.Factory(sp, roomGrain);
        }
    }
}
