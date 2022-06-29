using EverscaleNet.Client.Models;

namespace Notifon.Server.Business.Requests.EverClient;

public class EverDeployResult {
    public bool Success { get; init; }
    public string Error { get; init; }
    public decimal Balance { get; set; }
    public string Address { get; set; }
    public KeyPair KeyPair { get; set; }
}