namespace Notifon.Client.MessageSender;

internal class SendMessageResult {
    public bool Success { get; init; }
    public string[] Messages { get; init; }
    public string Error { get; init; }
    public decimal Balance { get; set; }
    public string Address { get; set; }
}