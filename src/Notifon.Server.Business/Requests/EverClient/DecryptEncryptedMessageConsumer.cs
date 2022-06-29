using System;
using System.Threading.Tasks;
using EverscaleNet.Abstract;
using EverscaleNet.Client.Models;
using MassTransit;
using Notifon.Server.Business.Models;
using Notifon.Server.Utils;

namespace Notifon.Server.Business.Requests.EverClient;

public class DecryptEncryptedMessageConsumer : IConsumer<DecryptEncryptedMessage> {
    private const string ServerPublicKey = "a36bf515ee8de6b79d30b294bbe7162f5e2a45b95ea97e4baebab8873492ee43";

    private readonly IEverClient _everClient;

    public DecryptEncryptedMessageConsumer(IEverClient everClient) {
        _everClient = everClient;
    }

    public async Task Consume(ConsumeContext<DecryptEncryptedMessage> context) {
        var encryptedMessage = context.Message.EncryptedMessage;
        var secretKey = context.Message.SecretKey;

        var result = await _everClient.Crypto.NaclBoxOpen(new ParamsOfNaclBoxOpen {
            Encrypted = encryptedMessage.Message,
            Nonce = Convert.FromBase64String(encryptedMessage.Nonce).ToHexString(),
            Secret = secretKey,
            TheirPublic = ServerPublicKey
        });

        await context.RespondAsync(new DecryptedMessage {
            Text = result.Decrypted.StringFromBase64()
        });
    }
}