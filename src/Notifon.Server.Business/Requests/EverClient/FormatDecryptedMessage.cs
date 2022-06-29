using Notifon.Server.Business.Models;

namespace Notifon.Server.Business.Requests.EverClient;

public interface FormatDecryptedMessage {
    DecryptedMessage DecryptedMessage { get; }
    string Format { get; }
}