using System;
using System.Numerics;
using System.Threading.Tasks;
using EverscaleNet.Abstract;
using EverscaleNet.Client.Models;
using EverscaleNet.Serialization;
using MassTransit;
using Notifon.Server.Utils;

namespace Notifon.Server.Business.Requests.EverClient;

public class EverSendMessageConsumer : IConsumer<EverSendMessage> {
    private const string SafeMultisigWallet = "SafeMultisigWallet";
    private const string Transfer = "transfer";
    private readonly IRequestClient<EverDeploy> _everDeployClient;
    private readonly IEverClient _everClient;
    private readonly IEverPackageManager _everPackageManager;

    public EverSendMessageConsumer(IEverClient everClient, IEverPackageManager everPackageManager, IRequestClient<EverDeploy> everDeployClient) {
        _everClient = everClient;
        _everPackageManager = everPackageManager;
        _everDeployClient = everDeployClient;
    }

    public async Task Consume(ConsumeContext<EverSendMessage> context) {
        var cancellationToken = context.CancellationToken;
        var phrase = context.Message.Phrase;
        var recipient = context.Message.Recipient;
        var message = context.Message.Message;

        var contract = await _everPackageManager.LoadPackage(SafeMultisigWallet, cancellationToken);
        var transferAbi = await _everPackageManager.LoadAbi(Transfer, cancellationToken);

        var deployResult = await _everDeployClient.GetResponse<EverDeployResult>(new {
            Phrase = phrase
        }, cancellationToken);
        var deployResultMessage = deployResult.Message;
        if (!deployResultMessage.Success) {
            await context.RespondAsync(new EverSendMessageResult {
                Success = false,
                Balance = deployResultMessage.Balance,
                Error = deployResultMessage.Error,
                Address = deployResultMessage.Address
            });
        }

        var address = deployResultMessage.Address;
        var keyPair = deployResultMessage.KeyPair;

        if (deployResultMessage.Balance <= (decimal)0.1) {
            await context.RespondAsync(new EverSendMessageResult {
                Success = false,
                Error = $"Balance of ${address} is too low for send message",
                Balance = deployResultMessage.Balance,
                Address = deployResultMessage.Address
            });
        }

        var body = await _everClient.Abi.EncodeMessageBody(new ParamsOfEncodeMessageBody {
            Abi = transferAbi,
            CallSet = new CallSet {
                FunctionName = "transfer",
                Input = new { comment = message.ToHexString() }.ToJsonElement()
            },
            IsInternal = true,
            Signer = new Signer.None()
        }, cancellationToken);

        var result = await _everClient.Processing.ProcessMessage(new ParamsOfProcessMessage {
            SendEvents = false,
            MessageEncodeParams = new ParamsOfEncodeMessage {
                Abi = contract.Abi,
                Address = deployResult.Message.Address,
                CallSet = new CallSet {
                    FunctionName = "submitTransaction",
                    Input = new {
                        dest = recipient,
                        value = 100_000_000,
                        bounce = false,
                        allBalance = false,
                        payload = body.Body
                    }.ToJsonElement()
                },
                Signer = new Signer.Keys { KeysAccessor = keyPair }
            }
        }, cancellationToken: cancellationToken);

        var accBalance = await _everClient.Net.QueryCollection(new ParamsOfQueryCollection {
            Collection = "accounts",
            Filter = new { id = new { eq = address } }.ToJsonElement(),
            Result = "balance"
        }, cancellationToken);

        var balance = new BigInteger(Convert.ToUInt64(accBalance.Result[0].Get<string>("balance"), 16)).ToDecimalBalance();

        await context.RespondAsync(new EverSendMessageResult {
            Success = true,
            Balance = balance,
            Address = address,
            Messages = result.Transaction.Get<string[]>("out_msgs")
        });
    }
}