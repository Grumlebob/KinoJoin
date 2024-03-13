using System.Net.Http.Json;
using Application.DTO;
using Domain.Entities;
using FluentAssertions;
using SimdLinq;

namespace Test.KinoJoin;

[Collection("KinoJoinCollection")]
public class KinoJoinTests : IAsyncLifetime
{
    private HttpClient _client;

    //Delegate used to call the ResetDatabaseAsync method from the factory, without having to expose the factory
    private readonly Func<Task> _resetDatabase;

    private readonly DataGenerator _dataGenerator = new();

    public KinoJoinTests(KinoJoinApiWebAppFactory factory)
    {
        _client = factory.HttpClient;
        _resetDatabase = factory.ResetDatabaseAsync;
    }

    [Fact]
    public async Task SimpleJoinEventTest()
    {
        const int casesToInsert = 10000;
        
        var joinEvents = _dataGenerator.JoinEventGenerator.Generate(casesToInsert);
        
        foreach (var joinEvent in joinEvents)
        {
            //Insert
            var createResponse = await _client.PutAsJsonAsync("api/events", joinEvent);
            createResponse.EnsureSuccessStatusCode();
            
            //get
            var createdId = await createResponse.Content.ReadAsStringAsync();
            var getResponse = await _client.GetAsync($"api/events/{createdId}");
            var joinEventFromApi = await getResponse.Content.ReadFromJsonAsync<JoinEvent>();

            joinEventFromApi.Should().NotBeNull();
            joinEventFromApi.Deadline.ToOADate().Should().Be(joinEvent.Deadline.ToOADate());
            joinEventFromApi.Title.Should().Be(joinEvent.Title); 
            joinEventFromApi.Description.Should().Be(joinEvent.Description);
        }
        
        //check count
        joinEvents.Count.Should().Be(casesToInsert);
        var getResponseAll = await _client.GetAsync("api/events");
        var joinEventsFromApi = await getResponseAll.Content.ReadFromJsonAsync<List<JoinEvent>>();
        joinEventsFromApi.Count.Should().Be(casesToInsert);
        
        //update JoinEvent
        var joinEventToUpdate= joinEventsFromApi[casesToInsert-1];
        joinEventToUpdate.Title = "Updated";
        //joinEventToUpdate.Participants.Add(new Participant { AuthId = "New", Email = "New", Nickname = "New", 
        //    VotedFor = [new ParticipantVote() {ShowtimeId = joinEventToUpdate.Showtimes.First().Id, SelectedOptionId = joinEventToUpdate.SelectOptions.First().Id}]});
        var updateResponse = await _client.PutAsJsonAsync("api/events", joinEventToUpdate);
        updateResponse.EnsureSuccessStatusCode();
        
        //check updated JoinEvent
        var getResponseUpdated = await _client.GetAsync($"api/events/{joinEventToUpdate.Id}");
        var joinEventFromApiUpdated = await getResponseUpdated.Content.ReadFromJsonAsync<JoinEvent>();
        joinEventFromApiUpdated.Should().NotBeNull();
        joinEventFromApiUpdated.Title.Should().Be(joinEventToUpdate.Title);
        //joinEventFromApiUpdated.Participants.Any(p => p.AuthId == "New").Should().BeTrue();
        
        /*
        //Update nested participant in JoinEvent
        var joinEventToUpdateParticipant = await _client.GetAsync($"api/events/{casesToInsert-1}");
        var joinEventToUpdateParticipantFromApi = await joinEventToUpdateParticipant.Content.ReadFromJsonAsync<JoinEvent>();
        joinEventToUpdateParticipantFromApi.Participants.First().Nickname = "Updated";
        var updateResponseParticipant = await _client.PutAsJsonAsync("api/events", joinEventToUpdateParticipantFromApi);
        updateResponseParticipant.EnsureSuccessStatusCode();
        
        //check nested updated participant in JoinEvent
        var getResponseUpdatedParticipant = await _client.GetAsync($"api/events/{casesToInsert-1}");
        var joinEventFromApiUpdatedParticipant = await getResponseUpdatedParticipant.Content.ReadFromJsonAsync<JoinEvent>();
        joinEventFromApiUpdatedParticipant.Should().NotBeNull();
        joinEventFromApiUpdatedParticipant.Participants.First().Nickname.Should().Be("Updated");
        
        */
    }
    

    //We don't care about the InitializeAsync method, but needed to implement the IAsyncLifetime interface
    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => _resetDatabase();
}
