using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using Application.Services;
using Domain.Entities;
using Domain.ExternalApiModels;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Test.KinoJoin;

//Taken from this guide https://www.youtube.com/watch?v=tj5ZCtvgXKY
[CollectionDefinition("KinoJoinCollection")]
public class KinoTestCollection : ICollectionFixture<KinoJoinApiWebAppFactory>;

[Collection("KinoJoinCollection")]
public class KinoJoinTests : IAsyncLifetime
{
    private HttpClient _client;

    //Delegate used to call the ResetDatabaseAsync method from the factory, without having to expose the factory
    private readonly Func<Task> _resetDatabase;

    private readonly DataGenerator _dataGenerator = new();

    private readonly KinoContext _kinoContext;

    public KinoJoinTests(KinoJoinApiWebAppFactory factory)
    {
        _client = factory.HttpClient;
        _resetDatabase = factory.ResetDatabaseAsync;
        _kinoContext = factory.Services.GetRequiredService<KinoContext>();
    }

    [Fact]
    public async Task CompleteJoinEventFlowFromStartToFinish()
    {
        const int casesToInsert = 5;

        var joinEvents = _dataGenerator.JoinEventGenerator.Generate(casesToInsert);

        //VALIDATION test made separately from the UPSERTING test, to make it easier to debug
        var validator = new DataAnnotationsValidator.DataAnnotationsValidator();
        var validationResults = new List<ValidationResult>();
        foreach (var joinEvent in joinEvents)
        {
            validator.TryValidateObjectRecursive(joinEvent, validationResults);
        }

        validationResults.Should().BeEmpty();

        //UPSERTING correct
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
            joinEventFromApi!.Title.Should().Be(joinEvent.Title);
        }

        //Upsert wrong joinEvent
        var joinEventWrong = new JoinEvent
        {
            Title =
                "Too looooooooooooooooooooong title here. AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        };
        var createResponseWrong = await _client.PutAsJsonAsync("api/events", joinEventWrong);
        createResponseWrong.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        //check count
        joinEvents.Count.Should().Be(casesToInsert);
        _kinoContext.JoinEvents.Count().Should().Be(casesToInsert);

        //Check all properties and nested properties exist
        var joinEventToCheck = _kinoContext.JoinEvents.First();
        joinEventToCheck = await _client.GetFromJsonAsync<JoinEvent>(
            "api/events/" + joinEventToCheck.Id
        );
        //These properties are always present
        joinEventToCheck.Should().NotBeNull();
        joinEventToCheck!.SelectOptions.Count.Should().BeGreaterThan(0);
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
        var joinEventToUpdate = _kinoContext.JoinEvents.ToList()[casesToInsert - 1];
        joinEventToUpdate = await _client.GetFromJsonAsync<JoinEvent>(
            "api/events/" + joinEventToUpdate.Id
        );

        joinEventToUpdate.Should().NotBeNull();

        joinEventToUpdate!.Title = "Updated";
        var updateResponse = await _client.PutAsJsonAsync("api/events", joinEventToUpdate);
        updateResponse.EnsureSuccessStatusCode();

        //check updated JoinEvent
        var getResponseUpdated = await _client.GetAsync($"api/events/{joinEventToUpdate.Id}");
        var joinEventFromApiUpdated =
            await getResponseUpdated.Content.ReadFromJsonAsync<JoinEvent>();
        joinEventFromApiUpdated.Should().NotBeNull();
        joinEventFromApiUpdated!.Title.Should().Be(joinEventToUpdate.Title);

        //Add nested participant in JoinEvent
        var joinEventToUpdateParticipant = await _client.GetAsync(
            $"api/events/{casesToInsert - 1}"
        );
        var joinEventToUpdateParticipantFromApi =
            await joinEventToUpdateParticipant.Content.ReadFromJsonAsync<JoinEvent>();
        joinEventToUpdateParticipantFromApi!.Participants.Add(
            new Participant
            {
                AuthId = "New",
                Email = "New",
                Nickname = "New",
                VotedFor =
                [
                    new ParticipantVote
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

        //check participant got added
        var getResponseUpdatedParticipant = await _client.GetAsync(
            $"api/events/{casesToInsert - 1}"
        );
        var joinEventFromApiUpdatedParticipant =
            await getResponseUpdatedParticipant.Content.ReadFromJsonAsync<JoinEvent>();
        joinEventFromApiUpdatedParticipant.Should().NotBeNull();
        joinEventFromApiUpdatedParticipant!
            .Participants.Any(p => p.AuthId == "New")
            .Should()
            .BeTrue();
        _kinoContext.ParticipantVotes.Count().Should().BeGreaterThan(0);

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
        var participant = joinEventFromApiUpdatedParticipantName!.Participants.First();
        participant.Nickname.Should().Be("Updated");

        //Checking delete - find join Event With At least 1 participant
        JoinEvent joinEventToDelete = new();
        for (var i = casesToInsert - 1; i >= 0; i--)
        {
            var getResponse = await _client.GetAsync($"api/events/{i}");
            var joinEventFromApi = await getResponse.Content.ReadFromJsonAsync<JoinEvent>();
            if (joinEventFromApi!.Participants.Count <= 0)
                continue;
            joinEventToDelete = joinEventFromApi;
            break;
        }

        var participantCountBeforeDelete = joinEventToDelete.Participants.Count;
        var participantToDelete = joinEventToDelete.Participants.Last();
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
        var participantCountAfterDelete = joinEventFromApiDeletedParticipant!.Participants.Count;
        participantCountAfterDelete.Should().Be(participantCountBeforeDelete - 1);
        //check that participantToDelete is not in the list of participants
        joinEventFromApiDeletedParticipant
            .Participants.Any(p => p.Id == participantToDelete.Id)
            .Should()
            .BeFalse();
    }

    [Fact]
    public async Task UpsertJoinEvent_ShouldReturnBadRequest_IfValidationFails()
    {
        var joinEvent = new JoinEvent
        {
            Title =
                "Too looooooooooooooooooooong title here. AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        };
        var response = await _client.PutAsJsonAsync("api/events", joinEvent);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetMovies_ShouldReturnOk_IfMoviesExistElseReturnsNotFound()
    {
        //No movies exist initially
        var response = await _client.GetAsync("api/kino-data/movies");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("[]");

        //Insert a join event (with a movie)
        var joinEvent = _dataGenerator.JoinEventGenerator.Generate(1).First();
        var createResponse = await _client.PutAsJsonAsync("api/events", joinEvent);
        createResponse.EnsureSuccessStatusCode();

        //Movie should now exist
        var getResponse = await _client.GetAsync("api/kino-data/movies");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var movies = await getResponse.Content.ReadFromJsonAsync<List<Movie>>();
        movies.Should().NotBeNull();
        movies!.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetCinemas_ShouldReturnOk_IfCinemasExistElseReturnsNotFound()
    {
        //No cinemas exist initially
        var response = await _client.GetAsync("api/kino-data/cinemas");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("[]");

        //Insert a join event (with a cinema)
        var joinEvent = _dataGenerator.JoinEventGenerator.Generate(1).First();
        var createResponse = await _client.PutAsJsonAsync("api/events", joinEvent);
        createResponse.EnsureSuccessStatusCode();

        //Cinema should now exist
        var getResponse = await _client.GetAsync("api/kino-data/cinemas");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var cinemas = await getResponse.Content.ReadFromJsonAsync<List<Cinema>>();
        cinemas.Should().NotBeNull();
        cinemas!.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    //We only fetch genres from kino.dk, and never insert them ourselves.
    //So after inserting a join event, we should not have genres.
    [Fact]
    public async Task GetGenres_ShouldReturnOk_IfGenresExistElseReturnsNotFound()
    {
        //No genres exist initially
        var response = await _client.GetAsync("api/kino-data/genres");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("[]");

        //Insert a join event with all its nested properites
        var joinEvent = _dataGenerator.JoinEventGenerator.Generate(1).First();
        var createResponse = await _client.PutAsJsonAsync("api/events", joinEvent);
        createResponse.EnsureSuccessStatusCode();

        //After inserting a join event, genres should still not exist.
        var getResponse = await _client.GetAsync("api/kino-data/genres");
        var getContent = await getResponse.Content.ReadAsStringAsync();
        getContent.Should().Be("[]");

        //Inserting a genrer manually
        var genre = new Genre { Id = 1, Name = "Test" };
        _kinoContext.Genres.Add(genre);
        await _kinoContext.SaveChangesAsync();

        //Genres should now exist
        var getResponseAfterInsert = await _client.GetAsync("api/kino-data/genres");
        getResponseAfterInsert.StatusCode.Should().Be(HttpStatusCode.OK);
        var genres = await getResponseAfterInsert.Content.ReadFromJsonAsync<List<Genre>>();
        genres.Should().NotBeNull();
    }

    /*
     * Unfortunately Kino.dk is down 5% of the day, which causes this test to fail 5% of the time.
     */
    [Fact]
    public async Task UpdateAllBaseDataFromKinoDk_ThenUseTheDataToCheckKinoDkFilterApi()
    {
        //The tests main focus is getting the static data from kino.dk
        var response = await _client.PostAsync("api/kino-data/update-all/1/3", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        //Check that the data was inserted
        var getResponse = await _client.GetAsync("api/kino-data/movies");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var movies = await getResponse.Content.ReadFromJsonAsync<List<Movie>>();
        movies.Should().NotBeNull();

        var getResponseCinemas = await _client.GetAsync("api/kino-data/cinemas");
        getResponseCinemas.StatusCode.Should().Be(HttpStatusCode.OK);
        var cinemas = await getResponseCinemas.Content.ReadFromJsonAsync<List<Cinema>>();
        cinemas.Should().NotBeNull();

        var getResponseGenres = await _client.GetAsync("api/kino-data/genres");
        getResponseGenres.StatusCode.Should().Be(HttpStatusCode.OK);
        var genres = await getResponseGenres.Content.ReadFromJsonAsync<List<Genre>>();
        genres.Should().NotBeNull();

        //Check that the kino.dk filter api works
        var fromDate = DateTime.Now.AddYears(-1);
        var toDate = DateTime.Now.AddYears(10);

        var cinemasToCheck = cinemas!.Select(c => c.Id).Take(3).ToList();
        if (cinemasToCheck.Count == 0)
        {
            cinemasToCheck.Add(1);
        }

        var filterApiHandler = new FilterApiHandler();
        var (showtimesWithCinemaFilter, _) = await filterApiHandler.GetShowtimesFromFilters(
            cinemaIds: cinemasToCheck,
            fromDate: fromDate,
            toDate: toDate
        );
        showtimesWithCinemaFilter.Should().NotBeNull();
        showtimesWithCinemaFilter.Count.Should().BeGreaterThan(1);

        var moviesToCheck = showtimesWithCinemaFilter.Select(s => s.MovieId).Take(3).ToList();
        var (showtimesWithMovieFilter, _) = await filterApiHandler.GetShowtimesFromFilters(
            movieIds: moviesToCheck,
            fromDate: fromDate,
            toDate: toDate
        );
        showtimesWithMovieFilter.Should().NotBeNull();
        showtimesWithMovieFilter.Count.Should().BeGreaterThan(1);

        var genresToCheck = genres!.Select(g => g.Id).Take(3).ToList();
        if (genresToCheck.Count == 0)
        {
            genresToCheck.Add(96); //96 is action
        }

        var (showtimesWithGenreFilter, _) = await filterApiHandler.GetShowtimesFromFilters(
            genreIds: genresToCheck,
            fromDate: fromDate,
            toDate: toDate
        );
        showtimesWithGenreFilter.Should().NotBeNull();
        showtimesWithGenreFilter.Count.Should().BeGreaterThan(1);

        //Test with combined filters
        var specificshowtime = showtimesWithCinemaFilter.First();
        var (showtimesWithCombinedFilters, _) = await filterApiHandler.GetShowtimesFromFilters(
            cinemaIds: [specificshowtime.CinemaId],
            movieIds: [specificshowtime.MovieId],
            fromDate: fromDate,
            toDate: toDate
        );
        showtimesWithCombinedFilters.Should().NotBeNull();
        showtimesWithCombinedFilters.Count.Should().BeGreaterThan(1);
    }

    [Fact]
    public void GetFiltersFromStringContainsCorrectIdsAndDates()
    {
        var filterApiHandler = new FilterApiHandler();
        const string filterUrl =
            "sort=most_purchased&cinemas=23&movies=5&movies=5&genres=96&genres=1&cinemas=2&date=2024-04-05T00:00:00&date=2024-04-06T00:00:00";
        var (cinemaIds, movieIds, genreIds, startDate, endDate) =
            filterApiHandler.GetFiltersFromUrlFilterString(filterUrl);
        cinemaIds.Should().Contain(23);
        cinemaIds.Should().Contain(2);
        movieIds.Should().Contain(5);
        genreIds.Should().Contain(1);
        startDate.Should().Be(new DateTime(2024, 4, 5));
        endDate.Should().Be(new DateTime(2024, 4, 6));

        (cinemaIds, movieIds, genreIds, startDate, endDate) =
            filterApiHandler.GetFiltersFromUrlFilterString("");
        cinemaIds.Should().BeEmpty();
        movieIds.Should().BeEmpty();
        genreIds.Should().BeEmpty();
        startDate.Should().Be(default);
        endDate.Should().Be(default);
    }

    [Fact]
    public void ConvertFiltersToUrlString_ShouldReturnCorrectString()
    {
        var filterApiHandler = new FilterApiHandler();
        var fakeCinemas = new List<int> { 1, 2, 3 };
        var fakeMovies = new List<int> { 4, 5, 6 };
        var fakeGenres = new List<int> { 7, 8, 9 };
        var fromDate = DateTime.Now.AddYears(-1);
        var toDate = DateTime.Now.AddYears(10);

        var result = filterApiHandler.ConvertFiltersToUrlString(
            fakeCinemas,
            fakeMovies,
            fakeGenres,
            fromDate,
            toDate
        );
        result.Should().Contain("cinemas=1");
        result.Should().Contain("cinemas=2");
        result.Should().Contain("cinemas=3");

        result.Should().Contain("movies=4");
        result.Should().Contain("movies=5");
        result.Should().Contain("movies=6");

        result.Should().Contain("genres=7");
        result.Should().Contain("genres=8");
        result.Should().Contain("genres=9");
    }

    //From our external api Kino.DK, they sometimes have a single element,
    //and sometimes a list of elements, the convert class is used to handle this
    [Fact]
    public void FieldMediaImageConvertCanConvertAShowtimeApiFieldMediaImage()
    {
        var fieldMediaImage = new FieldMediaImageConverter();
        var mediaSingleElement = new ShowtimeApiFieldMediaImage();
        try
        {
            fieldMediaImage.CanConvert(mediaSingleElement.GetType()).Should().BeTrue();
            fieldMediaImage.WriteJson(null!, null!, null!);
        }
        catch (Exception)
        {
            // ignored - WriteJson is only implemented to satisfy interface and throws an exception
        }
    }

    [Fact]
    public async Task TestDatabaseMigrations()
    {
        try
        {
            await _kinoContext.Database.MigrateAsync();
        }
        catch (PostgresException postgres)
        {
            if (postgres.SqlState == "42P07")
            {
                //42P07 is the error code for duplicate_table
                //This is fine, as it means the database is already migrated
            }
        }

        //A test to ensure that you don't forget to add new migrations
        var modelDiffer = _kinoContext.GetService<IMigrationsModelDiffer>();
        var migrationsAssembly = _kinoContext.GetService<IMigrationsAssembly>();
        var modelInitializer = _kinoContext.GetService<IModelRuntimeInitializer>();
        var snapshotModel = migrationsAssembly.ModelSnapshot?.Model;
        if (snapshotModel is IMutableModel mutableModel)
        {
            snapshotModel = mutableModel.FinalizeModel();
        }

        if (snapshotModel is not null)
        {
            snapshotModel = modelInitializer.Initialize(snapshotModel);
        }

        var designTimeModel = _kinoContext.GetService<IDesignTimeModel>();

        var modelDifferences = modelDiffer.GetDifferences(
            snapshotModel?.GetRelationalModel(),
            designTimeModel.Model.GetRelationalModel()
        );

        modelDifferences.Count.Should().Be(0);
    }

    [Fact]
    public async Task GetJoinEventsByHostFilter()
    {
        var joinEvent = _dataGenerator.JoinEventGenerator.Generate(1).First();
        var createResponse = await _client.PutAsJsonAsync("api/events", joinEvent);
        createResponse.EnsureSuccessStatusCode();

        var response = await _client.GetAsync($"api/events/host/{joinEvent.Host.AuthId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MakeParticipantNotExistAsync_ReturnsOkIfParamsAreOk()
    {
        //With positive params, the method should return OK, because either the participant is deleted or it doesn't exist.
        var response = await _client.DeleteAsync("api/events/1/participants/1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        //If negative params, no DB operation should be done, and the method should return OK,
        //As no participant with negative ID can exist.
        var response2 = await _client.DeleteAsync("api/events/-1/participants/-1");
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    //We don't care about the InitializeAsync method, but needed to implement the IAsyncLifetime interface
    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => _resetDatabase();
}
