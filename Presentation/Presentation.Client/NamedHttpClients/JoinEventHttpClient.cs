using System.Net.Http.Json;
using Application.DTO;

namespace Presentation.Client.NamedHttpClients;

public class JoinEventHttpClient : IJoinEventHttpClient
{
    private readonly HttpClient _httpClient;

    public JoinEventHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<HttpResponseMessage> PutJoinEventAsync(UpsertJoinEventDto upsertJoinEventDto)
    {
        return await _httpClient.PutAsJsonAsync("/api/events", upsertJoinEventDto);
    }
}

public interface IJoinEventHttpClient
{
    Task<HttpResponseMessage> PutJoinEventAsync(UpsertJoinEventDto upsertJoinEventDto);
}
