using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Providers;
using Vortex.Runtime;
using Vortex.Runtime.AssemblyProcessing;

namespace Vortex.Rooms.Object.Logic;

internal class RoomObjectLogicFeatureProcessor(IRoomObjectLogicProvider roomObjectLogicFactory)
    : IAssemblyFeatureProcessor
{
    private readonly IRoomObjectLogicProvider _roomObjectLogicFactory = roomObjectLogicFactory;

    public Task<IDisposable> ProcessAsync(
        Assembly asm,
        IServiceProvider sp,
        CancellationToken ct = default
    )
    {
        CompositeDisposable batch = new CompositeDisposable();

        foreach (Type? concrete in AssemblyExplorer.FindAssignees(asm, typeof(IRoomObjectLogic)))
        {
            if (concrete is null)
            {
                continue;
            }

            RoomObjectLogicAttribute? attribute =
                concrete.GetCustomAttribute<RoomObjectLogicAttribute>(false);

            if (attribute is null)
            {
                continue;
            }

            batch.Add(
                _roomObjectLogicFactory.RegisterLogic(
                    attribute.Key,
                    sp,
                    (sp, ctx) =>
                        (IRoomObjectLogic)ActivatorUtilities.CreateInstance(sp, concrete, ctx)
                )
            );
        }

        return Task.FromResult<IDisposable>(batch);
    }
}
