using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Users;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Users;

public class RespectUserMessageHandler(IGrainFactory grainFactory, IConfiguration configuration)
    : IMessageHandler<RespectUserMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly IConfiguration _configuration = configuration;

    public async ValueTask HandleAsync(
        RespectUserMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0 || message.UserId <= 0)
        {
            return;
        }

        int dailyLimit = _configuration.GetValue("Turbo:Rooms:DailyRespectLimit", 3);

        await _grainFactory
            .GetRoomGrain(ctx.RoomId)
            .RespectPlayerAsync(ctx.AsActionContext(), message.UserId, dailyLimit, ct)
            .ConfigureAwait(false);
    }
}
