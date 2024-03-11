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
        const int casesToInsert = 1;
        
        var joinEvents = _dataGenerator.JoinEventGenerator.Generate(casesToInsert);
        
        foreach (var joinEvent in joinEvents)
        {
            //Insert
            var upsertDto = UpsertJoinEventDto.FromModelToUpsertDto(joinEvent);
            var createResponse = await _client.PutAsJsonAsync("api/events", upsertDto);
            createResponse.EnsureSuccessStatusCode();
            
            //get
            var getResponse = await _client.GetAsync($"api/events/{await createResponse.Content.ReadAsStringAsync()}");
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
        
        
        //update
        var joinEventToUpdate= joinEventsFromApi[casesToInsert-1];
        joinEventToUpdate.Title = "Updated";
        joinEventToUpdate.Participants.Add(new Participant { AuthId = "New", Email = "New", Nickname = "New", VotedFor = []});
        var upsertDtoUpdated = UpsertJoinEventDto.FromModelToUpsertDto(joinEventToUpdate);
        var updateResponse = await _client.PutAsJsonAsync("api/events", upsertDtoUpdated);
        updateResponse.EnsureSuccessStatusCode();
        
        //check update
        var getResponseUpdated = await _client.GetAsync($"api/events/{joinEventToUpdate.Id}");
        var joinEventFromApiUpdated = await getResponseUpdated.Content.ReadFromJsonAsync<JoinEvent>();
        joinEventFromApiUpdated.Should().NotBeNull();
        joinEventFromApiUpdated.Title.Should().Be(joinEventToUpdate.Title);
        joinEventFromApiUpdated.Participants.Any(p => p.AuthId == "New").Should().BeTrue();
        
        
        var getResponseAll2 = await _client.GetAsync("api/events");
        var joinEventsFromApi2 = await getResponseAll.Content.ReadFromJsonAsync<List<JoinEvent>>();
        joinEventsFromApi2.Count.Should().Be(casesToInsert);
        

    }
    

    //We don't care about the InitializeAsync method, but needed to implement the IAsyncLifetime interface
    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => _resetDatabase();
}
