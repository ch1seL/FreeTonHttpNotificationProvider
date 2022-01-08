using System.ComponentModel.DataAnnotations;

namespace Notifon.Client.MessageSender;

internal class DeployRequest {
    [Required]
    public string Phrase { get; init; }
}