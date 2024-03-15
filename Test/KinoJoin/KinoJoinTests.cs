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
        const int casesToInsert = 10;

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
        joinEventFromApiUpdatedParticipantName.Participants.First().Nickname.Should().Be("Updated");
    }

    //We don't care about the InitializeAsync method, but needed to implement the IAsyncLifetime interface
    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => _resetDatabase();
}
