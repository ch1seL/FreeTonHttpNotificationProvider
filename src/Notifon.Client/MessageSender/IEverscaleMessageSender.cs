using System.Threading;
using System.Threading.Tasks;

namespace Notifon.Client.MessageSender;

internal interface IEverscaleMessageSender {
    Task<DeployResult> Deploy(DeployRequest deployRequest, CancellationToken cancellationToken);
    Task<SendMessageResult> SendMessage(SendMessageRequest sendMessageRequest, CancellationToken cancellationToken);
}