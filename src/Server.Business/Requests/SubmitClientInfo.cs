namespace Server.Business.Requests
{
    public interface SubmitClientInfo
    {
        string Hash { get; }
        string Endpoint { get; }
    }
}