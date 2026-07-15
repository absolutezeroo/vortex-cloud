using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Turbo.Logging;
using Turbo.Primitives;
using Turbo.Primitives.Rooms.Object;
using Turbo.Primitives.Rooms.Object.Logic;
using Turbo.Primitives.Rooms.Providers;
using Turbo.Rooms.Object.Logic;
using Turbo.Runtime;

namespace Turbo.Rooms.Providers;

public sealed class RoomObjectLogicProvider(
    IServiceProvider host,
    ILogger<RoomObjectLogicProvider> logger
) : IRoomObjectLogicProvider
{
    private readonly IServiceProvider _host = host;
    private readonly ILogger<RoomObjectLogicProvider> _logger = logger;
    private readonly ConcurrentDictionary<string, RoomObjectLogicReg> _logics = [];

    public IDisposable RegisterLogic(
        string logicType,
        IServiceProvider sp,
        Func<IServiceProvider, IRoomObjectContext, IRoomObjectLogic> factory
    )
    {
        RoomObjectLogicReg reg = new RoomObjectLogicReg(sp, factory);

        _logics[logicType] = reg;

        return new ActionDisposable(() =>
        {
            _logics.TryRemove(new KeyValuePair<string, RoomObjectLogicReg>(logicType, reg));
        });
    }

    public IRoomObjectLogic CreateLogicInstance(string logicType, IRoomObjectContext ctx)
    {
        if (!_logics.TryGetValue(logicType, out RoomObjectLogicReg? reg))
        {
            _logger.LogWarning(
                "Logic type '{LogicType}' not found, falling back to default_floor",
                logicType
            );
            reg = _logics.TryGetValue("default_floor", out RoomObjectLogicReg? defaultReg)
                ? defaultReg
                : null;
        }

        if (reg is null)
        {
            throw new TurboException(TurboErrorCodeEnum.InvalidLogic);
        }

        IServiceProvider sp = reg.ServiceProvider;

        if (sp != _host)
        {
            sp = new CompositeServiceProvider(sp, _host);
        }

        return reg.Factory(sp, ctx);
    }
}
