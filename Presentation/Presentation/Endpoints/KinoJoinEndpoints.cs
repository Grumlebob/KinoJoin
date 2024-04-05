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
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var eventGroup = app.MapGroup("api/events");
        eventGroup.MapGet("host/{hostId}", GetJoinEventsByHostId);
        eventGroup.MapGet("{id}", GetJoinEvent);
        eventGroup.MapPut("", UpsertJoinEvent);
        eventGroup.MapDelete("{joinEventId}/participants/{participantId}", MakeParticipantNotExist);

        var kinoDataGroup = app.MapGroup("api/kino-data");
        kinoDataGroup.MapGet("cinemas", GetCinemas);
        kinoDataGroup.MapGet("movies", GetMovies);
        kinoDataGroup.MapGet("genres", GetGenres);
        kinoDataGroup.MapPost("update-all/{fromId}/{toId}", UpdateAllBaseDataFromKinoDk);
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

        var result = await kinoJoinDbService.UpsertJoinEventAsync(joinEvent);
        return TypedResults.Ok(result);
    }

    private static async Task<
        Results<Ok<List<JoinEvent>>, NotFound, BadRequest<string>>
    > GetJoinEventsByHostId(
        [FromServices] IKinoJoinDbService kinoJoinDbService,
        [FromRoute] string hostId
    )
    {
        var joinEvents = await kinoJoinDbService.GetAllJoinEventsAsync(j => j.HostId == hostId);
        return TypedResults.Ok(joinEvents);
    }

    private static async Task<Results<NotFound, Ok<JoinEvent>, BadRequest<string>>> GetJoinEvent(
        [FromRoute] int id,
        [FromServices] IKinoJoinDbService kinoJoinDbService
    )
    {
        var joinEvent = await kinoJoinDbService.GetJoinEventAsync(id);
        return joinEvent == null ? TypedResults.NotFound() : TypedResults.Ok(joinEvent);
    }

    private static async Task<
        Results<Ok<ICollection<Cinema>>, NotFound, BadRequest<string>>
    > GetCinemas([FromServices] IKinoJoinDbService kinoKinoJoinService)
    {
        var cinemas = await kinoKinoJoinService.GetAllCinemasAsync();
        return TypedResults.Ok(cinemas);
    }

    private static async Task<
        Results<Ok<ICollection<Movie>>, NotFound, BadRequest<string>>
    > GetMovies([FromServices] IKinoJoinDbService kinoKinoJoinService)
    {
        var movies = await kinoKinoJoinService.GetAllMoviesAsync();
        return TypedResults.Ok(movies);
    }

    private static async Task<
        Results<Ok<ICollection<Genre>>, NotFound, BadRequest<string>>
    > GetGenres([FromServices] IKinoJoinDbService kinoKinoJoinService)
    {
        var genres = await kinoKinoJoinService.GetAllGenresAsync();
        return TypedResults.Ok(genres);
    }

    private static async Task<Results<Ok, NotFound, BadRequest<string>>> MakeParticipantNotExist(
        [FromRoute] int joinEventId,
        [FromRoute] int participantId,
        [FromServices] IKinoJoinDbService kinoJoinDbService
    )
    {
        if (joinEventId <= 0 || participantId <= 0)
        {
            return TypedResults.Ok();
        }

        await kinoJoinDbService.MakeParticipantNotExistAsync(joinEventId, participantId);
        return TypedResults.Ok();
    }

    private static async Task<Results<Ok, BadRequest<string>>> UpdateAllBaseDataFromKinoDk(
        [FromServices] IFetchNewestKinoDkDataService service,
        [FromRoute] int fromId,
        [FromRoute] int toId
    )
    {
        await service.UpdateBaseDataFromKinoDk(fromId, toId);
        return TypedResults.Ok();
    }
}
