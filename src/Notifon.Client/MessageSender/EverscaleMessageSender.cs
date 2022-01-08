using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using ch1seL.TonNet.Abstract;
using ch1seL.TonNet.Client.Models;
using ch1seL.TonNet.Serialization;
using Notifon.Server.Utils;

namespace Notifon.Client.MessageSender;

internal class EverscaleMessageSender : IEverscaleMessageSender {
    private const string SafeMultisigWallet = "SafeMultisigWallet";
    private const string Transfer = "transfer";

    private readonly ITonClient _tonClient;
    private readonly ITonPackageManager _tonPackageManager;

    public EverscaleMessageSender(ITonClient tonClient, ITonPackageManager tonPackageManager) {
        _tonClient = tonClient;
        _tonPackageManager = tonPackageManager;
    }

    public async Task<DeployResult> Deploy(DeployRequest deployRequest, CancellationToken cancellationToken) {
        var keyPair = await _tonClient.Crypto.MnemonicDeriveSignKeys(new ParamsOfMnemonicDeriveSignKeys { Phrase = deployRequest.Phrase },
                                                                     cancellationToken);

        var contract = await _tonPackageManager.LoadPackage(SafeMultisigWallet);

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

        var encodeMessage = await _tonClient.Abi.EncodeMessage(paramsOfEncodedMessage, cancellationToken);
        var address = encodeMessage.Address;

        var result = await _tonClient.Net.QueryCollection(new ParamsOfQueryCollection {
            Collection = "accounts",
            Filter = new { id = new { eq = address } }.ToJsonElement(),
            Result = "acc_type balance"
        }, cancellationToken);

        if (result.Result.Length == 0) {
            return new DeployResult {
                Success = false,
                Balance = 0,
                Error = $"You need to transfer at least 0.5 tokens for deploy to {address}",
                Address = address,
                KeyPair = keyPair
            };
        }

        var account = result.Result[0];
        var balance = new BigInteger(Convert.ToUInt64(account.Get<string>("balance"), 16)).ToDecimalBalance();
        var accType = account.Get<int>("acc_type");
        switch (accType) {
            case 0 when balance < (decimal)0.5:
                return new DeployResult {
                    Success = false,
                    Error = $"You need to transfer at least 0.5 tokens for deploy to {address}",
                    Balance = balance,
                    Address = address,
                    KeyPair = keyPair
                };
            case 1:
                return new DeployResult {
                    Success = true,
                    Balance = balance,
                    Address = address,
                    KeyPair = keyPair
                };
        }

        await _tonClient.Processing.ProcessMessage(new ParamsOfProcessMessage {
            SendEvents = false,
            MessageEncodeParams = paramsOfEncodedMessage
        }, cancellationToken: cancellationToken);

        var accBalance = await _tonClient.Net.QueryCollection(new ParamsOfQueryCollection {
            Collection = "accounts",
            Filter = new { id = new { eq = address } }.ToJsonElement(),
            Result = "balance"
        }, cancellationToken);

        balance = new BigInteger(Convert.ToUInt64(accBalance.Result[0].Get<string>("balance"), 16)).ToDecimalBalance();
        return new DeployResult {
            Success = true,
            Balance = balance,
            Address = address,
            KeyPair = keyPair
        };
    }

    public async Task<SendMessageResult> SendMessage(SendMessageRequest sendMessageRequest, CancellationToken cancellationToken) {
        var contract = await _tonPackageManager.LoadPackage(SafeMultisigWallet);
        var transferAbi = await _tonPackageManager.LoadAbi(Transfer);

        var deployResult = await Deploy(new DeployRequest { Phrase = sendMessageRequest.Phrase }, cancellationToken);
        if (!deployResult.Success) {
            return new SendMessageResult {
                Success = false,
                Balance = deployResult.Balance,
                Error = deployResult.Error,
                Address = deployResult.Address
            };
        }

        var address = deployResult.Address;
        var keyPair = deployResult.KeyPair;

        if (deployResult.Balance <= (decimal)0.1) {
            return new SendMessageResult {
                Success = false,
                Error = $"Balance of ${address} is too low for send message",
                Balance = deployResult.Balance,
                Address = deployResult.Address
            };
        }

        var body = await _tonClient.Abi.EncodeMessageBody(new ParamsOfEncodeMessageBody {
            Abi = transferAbi,
            CallSet = new CallSet {
                FunctionName = "transfer",
                Input = new { comment = sendMessageRequest.Message.ToHexString() }.ToJsonElement()
            },
            IsInternal = true,
            Signer = new Signer.None()
        }, cancellationToken);

        var result = await _tonClient.Processing.ProcessMessage(new ParamsOfProcessMessage {
            SendEvents = false,
            MessageEncodeParams = new ParamsOfEncodeMessage {
                Abi = contract.Abi,
                Address = deployResult.Address,
                CallSet = new CallSet {
                    FunctionName = "submitTransaction",
                    Input = new {
                        dest = sendMessageRequest.Recipient,
                        value = 100_000_000,
                        bounce = false,
                        allBalance = false,
                        payload = body.Body
                    }.ToJsonElement()
                },
                Signer = new Signer.Keys { KeysAccessor = keyPair }
            }
        }, cancellationToken: cancellationToken);

        var accBalance = await _tonClient.Net.QueryCollection(new ParamsOfQueryCollection {
            Collection = "accounts",
            Filter = new { id = new { eq = address } }.ToJsonElement(),
            Result = "balance"
        }, cancellationToken);

        var balance = new BigInteger(Convert.ToUInt64(accBalance.Result[0].Get<string>("balance"), 16)).ToDecimalBalance();

        return new SendMessageResult {
            Success = true,
            Balance = balance,
            Address = address,
            Messages = result.Transaction.Get<string[]>("out_msgs")
        };
    }
}