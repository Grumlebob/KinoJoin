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
    
    //get
    public async Task<List<JoinEvent>> GetJoinEventsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<JoinEvent>>("/api/events");
    }
    
    //get by id
    public async Task<JoinEvent?> GetJoinEventAsync(int id)
    {
        return await _httpClient.GetFromJsonAsync<JoinEvent>($"/api/events/{id}");
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
}

public interface IJoinEventHttpClient
{
    Task<HttpResponseMessage> PutJoinEventAsync(JoinEvent joinEvent);
    Task<List<JoinEvent>> GetJoinEventsAsync();
    Task<JoinEvent?> GetJoinEventAsync(int id);
    Task<HttpResponseMessage> DeleteParticipantAsync(int eventId, int participantId);
}
