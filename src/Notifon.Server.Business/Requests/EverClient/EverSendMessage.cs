using System.ComponentModel.DataAnnotations;

namespace Notifon.Server.Business.Requests.EverClient;

public class EverSendMessage {
    [Required]
    public string Phrase { get; init; }

    [Required]
    public string Recipient { get; init; }

    [Required]
    public string Message { get; init; }
}