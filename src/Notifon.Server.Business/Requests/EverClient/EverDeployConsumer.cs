using System;
using System.Numerics;
using System.Threading.Tasks;
using EverscaleNet.Abstract;
using EverscaleNet.Client.Models;
using EverscaleNet.Serialization;
using MassTransit;
using Notifon.Server.Utils;

namespace Notifon.Server.Business.Requests.EverClient;

public class EverDeployConsumer : IConsumer<EverDeploy> {
    private const string SafeMultisigWallet = "SafeMultisigWallet";

    private readonly IEverClient _everClient;
    private readonly IEverPackageManager _everPackageManager;

    public EverDeployConsumer(IEverClient everClient, IEverPackageManager everPackageManager) {
        _everClient = everClient;
        _everPackageManager = everPackageManager;
    }

    public async Task Consume(ConsumeContext<EverDeploy> context) {
        var cancellationToken = context.CancellationToken;
        var phrase = context.Message.Phrase;
        var keyPair = await _everClient.Crypto.MnemonicDeriveSignKeys(new ParamsOfMnemonicDeriveSignKeys { Phrase = phrase }, cancellationToken);

        var contract = await _everPackageManager.LoadPackage(SafeMultisigWallet, cancellationToken);

        var paramsOfEncodedMessage = new ParamsOfEncodeMessage {
            Abi = contract.Abi,
            DeploySet = new DeploySet {
                Tvc = contract.Tvc,
                InitialData = new { }.ToJsonElement()
            },
            CallSet = new CallSet {
                FunctionName = "constructor",
                Input = new { owners = new[] { $"0x{keyPair.Public}" }, reqConfirms = 0 }.ToJsonElement()
            },
            Signer = new Signer.Keys { KeysAccessor = keyPair },
            ProcessingTryIndex = 1
        };

        var encodeMessage = await _everClient.Abi.EncodeMessage(paramsOfEncodedMessage, cancellationToken);
        var address = encodeMessage.Address;

        var result = await _everClient.Net.QueryCollection(new ParamsOfQueryCollection {
            Collection = "accounts",
            Filter = new { id = new { eq = address } }.ToJsonElement(),
            Result = "acc_type balance"
        }, cancellationToken);

        if (result.Result.Length == 0) {
            await context.RespondAsync(new EverDeployResult {
                Success = false,
                Balance = 0,
                Error = $"You need to transfer at least 0.5 tokens for deploy to {address}",
                Address = address,
                KeyPair = keyPair
            });
            return;
        }

        var account = result.Result[0];
        var balance = new BigInteger(Convert.ToUInt64(account.Get<string>("balance"), 16)).ToDecimalBalance();
        var accType = account.Get<int>("acc_type");
        switch (accType) {
            case 0 when balance < (decimal)0.5:
                await context.RespondAsync(new EverDeployResult {
                    Success = false,
                    Error = $"You need to transfer at least 0.5 tokens for deploy to {address}",
                    Balance = balance,
                    Address = address,
                    KeyPair = keyPair
                });
                return;
            case 1:
                await context.RespondAsync(new EverDeployResult {
                    Success = true,
                    Balance = balance,
                    Address = address,
                    KeyPair = keyPair
                });
                return;
        }

        await _everClient.Processing.ProcessMessage(new ParamsOfProcessMessage {
            SendEvents = false,
            MessageEncodeParams = paramsOfEncodedMessage
        }, cancellationToken: cancellationToken);

        var accBalance = await _everClient.Net.QueryCollection(new ParamsOfQueryCollection {
            Collection = "accounts",
            Filter = new { id = new { eq = address } }.ToJsonElement(),
            Result = "balance"
        }, cancellationToken);

        balance = new BigInteger(Convert.ToUInt64(accBalance.Result[0].Get<string>("balance"), 16)).ToDecimalBalance();
        await context.RespondAsync(new EverDeployResult {
            Success = true,
            Balance = balance,
            Address = address,
            KeyPair = keyPair
        });
    }
}