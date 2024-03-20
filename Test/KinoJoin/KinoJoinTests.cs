using System.Net.Http.Json;
using Domain.Entities;
using FluentAssertions;

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
        const int casesToInsert = 100;

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
            joinEventFromApi.Title.Should().Be(joinEvent.Title);
        }

        //check count
        joinEvents.Count.Should().Be(casesToInsert);
        var getResponseAll = await _client.GetAsync("api/events");
        var joinEventsFromApi = await getResponseAll.Content.ReadFromJsonAsync<List<JoinEvent>>();
        joinEventsFromApi!.Count.Should().Be(casesToInsert);

        //Check all properties and nested properties exist
        var joinEventToCheck = joinEventsFromApi.FirstOrDefault();
        //These properties are always present
        joinEventToCheck.Should().NotBeNull();
        joinEventToCheck!.SelectOptions!.Count.Should().BeGreaterThan(0);
        joinEventToCheck.Showtimes.Count.Should().BeGreaterThan(0);
        joinEventToCheck.DefaultSelectOption.Should().NotBeNull();
        joinEventToCheck.Host.Should().NotBeNull();
        joinEventToCheck.Deadline.Should().BeAfter(DateTime.Now);
        joinEventToCheck.Id.Should().BeGreaterThan(0);
        joinEventToCheck.Title.Should().NotBeNullOrEmpty();

        //These properties are not always present
        if (joinEventToCheck.Participants is { Count: > 0 })
        {
            joinEventToCheck.Participants.Count.Should().BeGreaterThan(0);
            joinEventToCheck.Participants.First().VotedFor.Count.Should().BeGreaterThan(0);
        }
        if (joinEventToCheck.ChosenShowtimeId is not null)
        {
            joinEventToCheck.ChosenShowtimeId.Should().BeGreaterThan(0);
        }

        //update JoinEvent properties
        var joinEventToUpdate = joinEventsFromApi[casesToInsert - 1];
        joinEventToUpdate.Title = "Updated";
        var updateResponse = await _client.PutAsJsonAsync("api/events", joinEventToUpdate);
        updateResponse.EnsureSuccessStatusCode();

        //check updated JoinEvent
        var getResponseUpdated = await _client.GetAsync($"api/events/{joinEventToUpdate.Id}");
        var joinEventFromApiUpdated =
            await getResponseUpdated.Content.ReadFromJsonAsync<JoinEvent>();
        joinEventFromApiUpdated.Should().NotBeNull();
        joinEventFromApiUpdated.Title.Should().Be(joinEventToUpdate.Title);

        //Add nested participant in JoinEvent
        var joinEventToUpdateParticipant = await _client.GetAsync(
            $"api/events/{casesToInsert - 1}"
        );
        var joinEventToUpdateParticipantFromApi =
            await joinEventToUpdateParticipant.Content.ReadFromJsonAsync<JoinEvent>();
        joinEventToUpdateParticipantFromApi.Participants.Add(
            new Participant
            {
                AuthId = "New",
                Email = "New",
                Nickname = "New",
                VotedFor =
                [
                    new ParticipantVote()
                    {
                        ShowtimeId = joinEventToUpdate.Showtimes.First().Id,
                        SelectedOptionId = joinEventToUpdate.SelectOptions.First().Id,
                        SelectedOption = joinEventToUpdate.SelectOptions.First()
                    }
                ]
            }
        );
        var updateResponseParticipant = await _client.PutAsJsonAsync(
            "api/events",
            joinEventToUpdateParticipantFromApi
        );
        updateResponseParticipant.EnsureSuccessStatusCode();

        //check particpant got added
        var getResponseUpdatedParticipant = await _client.GetAsync(
            $"api/events/{casesToInsert - 1}"
        );
        var joinEventFromApiUpdatedParticipant =
            await getResponseUpdatedParticipant.Content.ReadFromJsonAsync<JoinEvent>();
        joinEventFromApiUpdatedParticipant.Should().NotBeNull();
        joinEventFromApiUpdatedParticipant
            .Participants.Any(p => p.AuthId == "New")
            .Should()
            .BeTrue();

        //Update the participant we just added to a new name
        joinEventFromApiUpdatedParticipant.Participants.First().Nickname = "Updated";
        var updateResponseParticipantName = await _client.PutAsJsonAsync(
            "api/events",
            joinEventFromApiUpdatedParticipant
        );
        updateResponseParticipantName.EnsureSuccessStatusCode();

        //check participant got updated
        var getResponseUpdatedParticipantName = await _client.GetAsync(
            $"api/events/{casesToInsert - 1}"
        );
        var joinEventFromApiUpdatedParticipantName =
            await getResponseUpdatedParticipantName.Content.ReadFromJsonAsync<JoinEvent>();
        joinEventFromApiUpdatedParticipantName.Should().NotBeNull();
        var participant = joinEventFromApiUpdatedParticipantName.Participants.First();
        participant.Nickname.Should().Be("Updated");
        
        //Test DeleteParticipantAsync. eventGroup.MapDelete("{eventId}/participants/{participantId}", DeleteParticipant);
        JoinEvent joinEventToDelete = new();
        //find joinEventWithAtleast 1 participant
        for (var i = casesToInsert - 1; i >= 0; i--)
        {
            var getResponse = await _client.GetAsync($"api/events/{i}");
            var joinEventFromApi = await getResponse.Content.ReadFromJsonAsync<JoinEvent>();
            if (joinEventFromApi.Participants.Count > 0)
            {
                joinEventToDelete = joinEventFromApi;
                break;
            }
        }
        //Get count of participants
        var participantCountBeforeDelete = joinEventToDelete.Participants.Count;
        //Get the last participant
        var participantToDelete = joinEventToDelete.Participants.Last();
        //Delete the participant
        var deleteParticipantResponse = await _client.DeleteAsync(
            $"api/events/{joinEventToDelete.Id}/participants/{participantToDelete.Id}"
        );
        deleteParticipantResponse.EnsureSuccessStatusCode();
        
        //Check that the participant got deleted
        var getResponseDeletedParticipant = await _client.GetAsync(
            $"api/events/{joinEventToDelete.Id}"
        );
        var joinEventFromApiDeletedParticipant =
            await getResponseDeletedParticipant.Content.ReadFromJsonAsync<JoinEvent>();
        var participantCountAfterDelete = joinEventFromApiDeletedParticipant.Participants.Count;
        participantCountAfterDelete.Should().Be(participantCountBeforeDelete - 1);
        //check that participantToDelete is not in the list of participants
        joinEventFromApiDeletedParticipant
            .Participants.Any(p => p.Id == participantToDelete.Id)
            .Should()
            .BeFalse();
    }

    //We don't care about the InitializeAsync method, but needed to implement the IAsyncLifetime interface
    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => _resetDatabase();
}
