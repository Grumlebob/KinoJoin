using Domain;

namespace Presentation.Client.NamedHttpClients;

public class KinoJoinHttpClient
{
    private readonly HttpClient _httpClient;

    public KinoJoinHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<JoinEvent?> GetJoinEventAsync(int id)
    {
        var result = await _httpClient.GetAsync($"/api/events/{id}");
        return result.IsSuccessStatusCode
            ? await result.Content.ReadFromJsonAsync<JoinEvent>()
            : null;
    }

    public async Task<HttpResponseMessage> UpsertJoinEventAsync(JoinEvent joinEvent)
    {
        return await _httpClient.PutAsJsonAsync("/api/events", joinEvent);
    }

    public async Task<HttpResponseMessage> MakeParticipantNotExistsAsync(
        int joinEventId,
        int participantId
    )
    {
        return await _httpClient.DeleteAsync(
            $"/api/events/{joinEventId}/participants/{participantId}"
        );
    }

    public async Task<ICollection<Cinema>?> GetCinemasAsync()
    {
        return await _httpClient.GetFromJsonAsync<ICollection<Cinema>>("api/kino-data/cinemas");
    }

    public async Task<ICollection<Movie>?> GetMoviesAsync()
    {
        return await _httpClient.GetFromJsonAsync<ICollection<Movie>>("api/kino-data/movies");
    }

    public async Task<ICollection<Genre>?> GetGenresAsync()
    {
        return await _httpClient.GetFromJsonAsync<ICollection<Genre>>("api/kino-data/genres");
    }

    /// <remarks>Both ids are inclusive</remarks>
    public async Task<HttpResponseMessage> UpdateBaseDataFromKinoDkAsync(int fromId, int toId)
    {
        return await _httpClient.PostAsync($"api/kino-data/update-all/{fromId}/{toId}", null);
    }
}
