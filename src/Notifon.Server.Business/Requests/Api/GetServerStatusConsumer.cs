using System.Threading.Tasks;
using EverscaleNet.Abstract;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Notifon.Server.Database;

namespace Notifon.Server.Business.Requests.Api;

public class GetServerStatusConsumer : IConsumer<GetServerStatus> {
    private readonly ServerDbContext _serverDbContext;
    private readonly IEverClient _everClient;

    public GetServerStatusConsumer(ServerDbContext serverDbContext, IEverClient everClient) {
        _serverDbContext = serverDbContext;
        _everClient = everClient;
    }

    public async Task Consume(ConsumeContext<GetServerStatus> context) {
        var cancellationToken = context.CancellationToken;

        var userCount = await _serverDbContext.Users.CountAsync(cancellationToken);
        var endpointCount = await _serverDbContext.Endpoints.CountAsync(cancellationToken);
        var tonEndpoints = await _everClient.Net.GetEndpoints(cancellationToken);

        await context.RespondAsync<GetServerStatusResult>(new {
            UserCount = userCount,
            EndpointCount = endpointCount,
            TonEndpoints = tonEndpoints.Endpoints
        });
    }
}