namespace Notifon.Server.Business.Requests.EverClient;

public class EverSendMessageResult {
    public bool Success { get; init; }
    public string[] Messages { get; init; }
    public string Error { get; init; }
    public decimal Balance { get; set; }
    public string Address { get; set; }
}