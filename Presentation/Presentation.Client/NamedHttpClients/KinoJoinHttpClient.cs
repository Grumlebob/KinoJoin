namespace Presentation.Client.NamedHttpClients;

public class KinoJoinHttpClient : HttpClient
{
    public HttpClient Client { get; }

    public KinoJoinHttpClient(HttpClient client)
    {
        Client = client;
    }
}
