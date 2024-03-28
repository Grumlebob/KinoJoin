using System.Linq.Expressions;
using Application.Interfaces;
using Carter;
using Domain.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace Presentation.Endpoints;

public class KinoJoinEndpoints : ICarterModule
{
    private const string DefaultErrorMessage =
        "Sorry, we encountered an unexpected issue while processing your request. Please ensure that the data is correct. We suggest you try again later or contact support if the problem persists.";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var eventGroup = app.MapGroup("api/events");
        eventGroup.MapGet("host/{hostId}", GetJoinEventsByHostId);
        eventGroup.MapGet("{id}", GetJoinEvent);
        eventGroup.MapGet("", GetAllJoinEvent);
        eventGroup.MapPut("", UpsertJoinEvent);
        eventGroup.MapDelete("{eventId}/participants/{participantId}", DeleteParticipant);

        var kinoDataGroup = app.MapGroup("api/kino-data");
        kinoDataGroup.MapGet("cinemas", GetCinemas);
        kinoDataGroup.MapGet("movies", GetMovies);
        kinoDataGroup.MapGet("genres", GetGenres);
        kinoDataGroup.MapPost("update-all", UpdateAllBaseDataFromKinoDk);
    }

    private static async Task<Results<Ok<int>, BadRequest<string>>> UpsertJoinEvent(
        [FromBody] JoinEvent joinEvent,
        [FromServices] IKinoJoinDbService kinoJoinDbService
    )
    {
        var validator = new DataAnnotationsValidator.DataAnnotationsValidator();
        var validationResults = new List<ValidationResult>();
        validator.TryValidateObjectRecursive(joinEvent, validationResults);

        if (validationResults.Any())
        {
            var errorMessage = String.Join(
                " ",
                validationResults.Select(x => x.ErrorMessage).ToList()
            );
            return TypedResults.BadRequest(errorMessage);
        }

        try
        {
            var result = await kinoJoinDbService.UpsertJoinEventAsync(joinEvent);
            return TypedResults.Ok(result);
        }
        catch (Exception)
        {
            return TypedResults.BadRequest(DefaultErrorMessage);
        }
    }

    private static async Task<Results<Ok<List<JoinEvent>>, BadRequest<string>>> GetAllJoinEvent(
        [FromServices] IKinoJoinDbService kinoJoinDbService
    )
    {
        try
        {
            var joinEvents = await kinoJoinDbService.GetAllAsync();
            return TypedResults.Ok(joinEvents);
        }
        catch (Exception)
        {
            return TypedResults.BadRequest(DefaultErrorMessage);
        }
    }

    private static async Task<
        Results<Ok<List<JoinEvent>>, NotFound, BadRequest<string>>
    > GetJoinEventsByHostId(
        [FromServices] IKinoJoinDbService kinoJoinDbService,
        [FromRoute] string hostId
    )
    {
        try
        {
            var joinEvents = await kinoJoinDbService.GetAllAsync(j => j.HostId == hostId);
            return TypedResults.Ok(joinEvents);
        }
        catch (Exception)
        {
            return TypedResults.BadRequest(DefaultErrorMessage);
        }
    }

    private static async Task<Results<NotFound, Ok<JoinEvent>, BadRequest<string>>> GetJoinEvent(
        [FromRoute] int id,
        [FromServices] IKinoJoinDbService kinoJoinDbService
    )
    {
        try
        {
            var joinEvent = await kinoJoinDbService.GetAsync(id);
            return joinEvent == null ? TypedResults.NotFound() : TypedResults.Ok(joinEvent);
        }
        catch (Exception)
        {
            return TypedResults.BadRequest(DefaultErrorMessage);
        }
    }

    private static async Task<
        Results<Ok<ICollection<Cinema>>, NotFound, BadRequest<string>>
    > GetCinemas([FromServices] IKinoJoinDbService kinoKinoJoinService)
    {
        try
        {
            var cinemas = await kinoKinoJoinService.GetAllCinemas();
            return TypedResults.Ok(cinemas);
        }
        catch (Exception)
        {
            return TypedResults.BadRequest(DefaultErrorMessage);
        }
    }

    private static async Task<
        Results<Ok<ICollection<Movie>>, NotFound, BadRequest<string>>
    > GetMovies([FromServices] IKinoJoinDbService kinoKinoJoinService)
    {
        try
        {
            var movies = await kinoKinoJoinService.GetAllMovies();
            return TypedResults.Ok(movies);
        }
        catch (Exception)
        {
            return TypedResults.BadRequest(DefaultErrorMessage);
        }
    }

    private static async Task<
        Results<Ok<ICollection<Genre>>, NotFound, BadRequest<string>>
    > GetGenres([FromServices] IKinoJoinDbService kinoKinoJoinService)
    {
        try
        {
            var genres = await kinoKinoJoinService.GetAllGenres();
            return TypedResults.Ok(genres);
        }
        catch (Exception)
        {
            return TypedResults.BadRequest(DefaultErrorMessage);
        }
    }

    private static async Task<Results<Ok, NotFound, BadRequest<string>>> DeleteParticipant(
        [FromRoute] int eventId,
        [FromRoute] int participantId,
        [FromServices] IKinoJoinDbService kinoJoinDbService
    )
    {
        if (eventId <= 0 || participantId <= 0)
        {
            return TypedResults.Ok();
        }

        try
        {
            await kinoJoinDbService.MakeParticipantNotExistAsync(eventId, participantId);
            return TypedResults.Ok();
        }
        catch (Exception)
        {
            return TypedResults.BadRequest(DefaultErrorMessage);
        }
    }

    private static async Task<Results<Ok, BadRequest<string>>> UpdateAllBaseDataFromKinoDk(
        [FromServices] IFetchNewestKinoDkDataService service
    )
    {
        try
        {
            await service.UpdateBaseDataFromKinoDk(1, 71);
            return TypedResults.Ok();
        }
        catch (Exception e)
        {
            return TypedResults.BadRequest(DefaultErrorMessage);
        }
    }
}
