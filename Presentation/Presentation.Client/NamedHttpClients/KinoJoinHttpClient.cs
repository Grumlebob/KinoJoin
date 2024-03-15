using System.Net.Http.Json;
using Domain.Entities;

namespace Presentation.Client.NamedHttpClients;

public class KinoJoinHttpClient : HttpClient
{
    private readonly HttpClient _httpClient;

    public KinoJoinHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<HttpResponseMessage> PutJoinEventAsync(JoinEvent joinEvent)
    {
        return await _httpClient.PutAsJsonAsync("/api/events", joinEvent);
    }
}
