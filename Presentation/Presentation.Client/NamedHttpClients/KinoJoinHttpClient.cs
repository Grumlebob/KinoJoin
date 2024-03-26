using System.Net.Http.Json;
using Domain.Entities;

namespace Presentation.Client.NamedHttpClients;

public interface IKinoJoinHttpClient
{
    Task<List<JoinEvent>?> GetJoinEventsAsync();
    Task<JoinEvent?> GetJoinEventAsync(int id);
    Task<HttpResponseMessage> PutJoinEventAsync(JoinEvent joinEvent);
    Task<HttpResponseMessage> DeleteParticipantAsync(int eventId, int participantId);
    Task<ICollection<Cinema>?> GetCinemasAsync();
    Task<ICollection<Movie>?> GetMoviesAsync();
    Task<ICollection<Genre>?> GetGenresAsync();
    Task<HttpResponseMessage> UpdateBaseDataFromKinoDkAsync();
}

public class KinoJoinHttpClient : IKinoJoinHttpClient
{
    private readonly HttpClient _httpClient;

    public KinoJoinHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    //get
    public async Task<List<JoinEvent>?> GetJoinEventsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<JoinEvent>>("/api/events");
    }

    //get by id
    public async Task<JoinEvent?> GetJoinEventAsync(int id)
    {
        var result = await _httpClient.GetAsync($"/api/events/{id}");
        return result.IsSuccessStatusCode
            ? await result.Content.ReadFromJsonAsync<JoinEvent>()
            : null;
    }

    public async Task<HttpResponseMessage> PutJoinEventAsync(JoinEvent joinEvent)
    {
        return await _httpClient.PutAsJsonAsync("/api/events", joinEvent);
    }

    //delete participant
    public async Task<HttpResponseMessage> DeleteParticipantAsync(int eventId, int participantId)
    {
        return await _httpClient.DeleteAsync($"/api/events/{eventId}/participants/{participantId}");
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

    public async Task<HttpResponseMessage> UpdateBaseDataFromKinoDkAsync()
    {
        return await _httpClient.PostAsync("api/kino-data/update-all", null);
    }
}
