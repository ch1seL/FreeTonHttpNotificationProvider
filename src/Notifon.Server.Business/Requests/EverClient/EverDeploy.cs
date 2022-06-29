using System.ComponentModel.DataAnnotations;

namespace Notifon.Server.Business.Requests.EverClient;

public class EverDeploy {
    [Required]
    public string Phrase { get; init; }
}