using Notifon.Server.Business.Models;

namespace Notifon.Server.Business.Requests.EverClient;

public interface DecryptEncryptedMessage {
    EncryptedMessage EncryptedMessage { get; set; }
    string SecretKey { get; set; }
}