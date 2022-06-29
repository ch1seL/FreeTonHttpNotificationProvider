using System;
using System.Threading;
using System.Threading.Tasks;
using EverscaleNet.Models;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Notifon.Server.Business.Requests.EverClient;
using NSwag.Annotations;

namespace Notifon.Server.Controllers.Api;

[ApiController]
[OpenApiTag("API")]
[Route("api/everscale")]
public class ApiEverController : ControllerBase {
    private readonly IRequestClient<EverDeploy> _everDeployClient;
    private readonly IRequestClient<EverSendMessage> _everSendMessageClient;

    public ApiEverController(IRequestClient<EverSendMessage> everSendMessageClient, IRequestClient<EverDeploy> everDeployClient) {
        _everSendMessageClient = everSendMessageClient;
        _everDeployClient = everDeployClient;
    }

    [HttpPost("send-message")]
    public async Task<EverSendMessageResult> SendMessage(EverSendMessage request, CancellationToken cancellationToken) {
        try {
            var response = await _everSendMessageClient
                               .GetResponse<EverSendMessageResult>(request, cancellationToken);
            return response.Message;
        } catch (Exception e) when (e.InnerException is EverClientException) {
            return new EverSendMessageResult {
                Success = false,
                Error = e.InnerException.Message
            };
        }
    }

    [HttpPost("deploy")]
    public async Task<EverDeployResult> Deploy(EverDeploy request, CancellationToken cancellationToken) {
        try {
            var response = await _everDeployClient
                               .GetResponse<EverDeployResult>(request, cancellationToken);
            return response.Message;
        } catch (Exception e) when (e.InnerException is EverClientException) {
            return new EverDeployResult {
                Success = false,
                Error = e.InnerException.Message
            };
        }
    }
}