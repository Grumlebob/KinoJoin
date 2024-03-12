using System.Net.Http.Json;
using Domain.Entities;

namespace Presentation.Client.NamedHttpClients;

public class JoinEventHttpClient : IJoinEventHttpClient
{
    private readonly HttpClient _httpClient;

    public JoinEventHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<HttpResponseMessage> PutJoinEventAsync(JoinEvent joinEvent)
    {
        return await _httpClient.PutAsJsonAsync("/api/events", joinEvent);
    }
}

public interface IJoinEventHttpClient
{
    Task<HttpResponseMessage> PutJoinEventAsync(JoinEvent joinEvent);
}
