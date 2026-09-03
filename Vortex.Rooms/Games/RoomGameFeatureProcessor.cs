using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vortex.Rooms.Games.Abstractions;
using Vortex.Rooms.Providers;
using Vortex.Runtime;
using Vortex.Runtime.AssemblyProcessing;

namespace Vortex.Rooms.Games;

/// <summary>
/// The extension point that makes "adding a game touches no core file" true: an assembly — this one,
/// or a plugin — contributes an <see cref="IRoomGame"/> by marking it <c>[RoomGame]</c>, and every
/// room from then on hosts it.
/// </summary>
internal sealed class RoomGameFeatureProcessor(
    IRoomGameProvider roomGameProvider,
    ILogger<RoomGameFeatureProcessor> logger
) : IAssemblyFeatureProcessor
{
    private readonly IRoomGameProvider _roomGameProvider = roomGameProvider;
    private readonly ILogger _logger = logger;

    public Task<IDisposable> ProcessAsync(
        Assembly asm,
        IServiceProvider sp,
        CancellationToken ct = default
    )
    {
        CompositeDisposable batch = new CompositeDisposable();

        foreach (
            Type? concrete in AssemblyExplorer.FindAssignees(asm, typeof(IRoomGame), WarnNonPublic)
        )
        {
            if (concrete is null || concrete.GetCustomAttribute<RoomGameAttribute>(false) is null)
            {
                continue;
            }

            Type gameType = concrete;

            batch.Add(
                _roomGameProvider.RegisterGame(
                    sp,
                    (services, context) =>
                        (IRoomGame)ActivatorUtilities.CreateInstance(services, gameType, context)
                )
            );
        }

        return Task.FromResult<IDisposable>(batch);
    }

    private void WarnNonPublic(Type concrete)
    {
        if (concrete.GetCustomAttribute<RoomGameAttribute>(false) is null)
        {
            return;
        }

        _logger.LogWarning(
            "{Type} is marked [RoomGame] but is not public, so it was not registered and will "
                + "never run in any room. Make the type public.",
            concrete.FullName
        );
    }
}
