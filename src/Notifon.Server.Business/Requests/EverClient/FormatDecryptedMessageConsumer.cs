using System;
using System.Text.Json;
using System.Threading.Tasks;
using EverscaleNet.Abstract;
using EverscaleNet.Client.Models;
using EverscaleNet.Models;
using EverscaleNet.Serialization;
using MassTransit;
using Notifon.Server.Business.Models;
using Notifon.Server.Utils;

namespace Notifon.Server.Business.Requests.EverClient;

public class FormatDecryptedMessageConsumer : IConsumer<FormatDecryptedMessage> {
    private const string TransferContract = "transfer";
    private const string SafeMultisigWalletContract = "SafeMultisigWallet";
    private const string EmptyBody = "te6ccgEBAQEAAgAAAA==";
    private readonly IEverClient _everClient;
    private readonly IEverPackageManager _everPackageManager;

    public FormatDecryptedMessageConsumer(IEverClient everClient, IEverPackageManager everPackageManager) {
        _everClient = everClient;
        _everPackageManager = everPackageManager;
    }

    public async Task Consume(ConsumeContext<FormatDecryptedMessage> context) {
        var message = context.Message.DecryptedMessage;
        var format = context.Message.Format;
        var cancellationToken = context.CancellationToken;

        if (format.Equals("body", StringComparison.OrdinalIgnoreCase)) {
            var msg = JsonDocument.Parse(message.Text).RootElement;
            var isInternal = msg.Get<int>("msg_type") == 0;

            var abi = isInternal
                          ? await _everPackageManager.LoadAbi(TransferContract, cancellationToken)
                          : await _everPackageManager.LoadAbi(SafeMultisigWalletContract, cancellationToken);

            if (msg.TryGetProperty("body", out var bodyJson) && bodyJson.GetString() == EmptyBody) {
                await context.RespondAsync<FormattedMessage>(new { Text = "<Empty comment>" });
                return;
            }

            try {
                var messageBody = await _everClient.Abi.DecodeMessage(new ParamsOfDecodeMessage {
                    Abi = abi,
                    Message = msg.Get<string>("boc")
                }, cancellationToken);

                var text = isInternal
                           && messageBody.Value.HasValue
                           && messageBody.Value.Value.TryGetProperty("comment", out var comment)
                               ? comment.GetString().HexToString()
                               : messageBody.Value.ToString();

                await context.RespondAsync<FormattedMessage>(new { Text = text });
                return;
            } catch (EverClientException ex) when (ex.Code == (int)AbiErrorCode.InvalidMessage) { }
        }

        await context.RespondAsync<DummyResponse>(new { });
    }
}