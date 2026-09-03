using System;
using System.Collections.Generic;
using Vortex.Rooms.Games.Abstractions;
using Vortex.Rooms.Games.Runtime;
using Vortex.Runtime;

namespace Vortex.Rooms.Providers;

/// <inheritdoc cref="IRoomGameProvider"/>
public sealed class RoomGameProvider(IServiceProvider host) : IRoomGameProvider
{
    private readonly IServiceProvider _host = host;
    private readonly List<RoomGameReg> _games = [];

    public IDisposable RegisterGame(
        IServiceProvider sp,
        Func<IServiceProvider, IRoomGameContext, IRoomGame> factory
    )
    {
        RoomGameReg reg = new(sp, factory);

        _games.Add(reg);

        return new ActionDisposable(() => _games.Remove(reg));
    }

    public void AttachGamesTo(RoomGameRuntime runtime)
    {
        foreach (RoomGameReg reg in _games)
        {
            IServiceProvider sp = reg.ServiceProvider;

            if (sp != _host)
            {
                sp = new CompositeServiceProvider(sp, _host);
            }

            IServiceProvider resolved = sp;

            runtime.Register(context => reg.Factory(resolved, context));
        }
    }

    private sealed record RoomGameReg(
        IServiceProvider ServiceProvider,
        Func<IServiceProvider, IRoomGameContext, IRoomGame> Factory
    );
}
