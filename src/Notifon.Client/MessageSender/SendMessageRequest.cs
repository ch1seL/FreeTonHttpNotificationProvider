using System.ComponentModel.DataAnnotations;

namespace Notifon.Client.MessageSender;

internal class SendMessageRequest {
    [Required]
    public string Phrase { get; init; }

    [Required]
    public string Recipient { get; init; }

    [Required]
    public string Message { get; init; }
}