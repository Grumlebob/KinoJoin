using Application.Interfaces;
using Carter;
using Domain.Entities;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Endpoints;

public class KinoEndpoints : ICarterModule
{
    private const string DefaultErrorMessage =
        "Sorry, we encountered an unexpected issue while processing your request. Please ensure that the data is correct. We suggest you try again later or contact support if the problem persists.";
    
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var eventGroup = app.MapGroup("api/events");
        eventGroup.MapPut("", UpsertJoinEvent);
        eventGroup.MapGet("", GetJoinEvents);
        eventGroup.MapGet("{id}", GetJoinEvent);
        group.MapDelete("{eventId}/participants/{participantId}", DeleteParticipant);

        var kinoDataGroup = app.MapGroup("api/kino-data");
        kinoDataGroup.MapGet("cinemas", GetCinemas);
        kinoDataGroup.MapGet("movies", GetMovies);
        kinoDataGroup.MapGet("genres", GetGenres);

    }

    //Result<> is a union type, that can be all the different responses we can return, so it is easier to test.
    private static async Task<Results<Ok<int>, BadRequest<string>>> UpsertJoinEvent(
        [FromBody] JoinEvent joinEvent,
        [FromServices] IJoinEventService joinEventService
    )
    {
        try
        {
            var result = await joinEventService.UpsertJoinEventAsync(joinEvent);
            return TypedResults.Ok(result);
        }
        catch (Exception)
        {
            return TypedResults.BadRequest(
                DefaultErrorMessage
            );
        }
    }

    private static async Task<Results<Ok<List<JoinEvent>>, NotFound, BadRequest<string>>> GetJoinEvents(
        [FromServices] IJoinEventService joinEventService)
    {
        try
        {
            var joinEvents = await joinEventService.GetAllAsync();
            if (joinEvents.Count == 0)
                return TypedResults.NotFound();
            return TypedResults.Ok(joinEvents);
        }
        catch (Exception)
        {
            return TypedResults.BadRequest(
                DefaultErrorMessage
            );
        }
    }

    private static async Task<Results<NotFound, Ok<JoinEvent>, BadRequest<string>>> GetJoinEvent(
        [FromRoute] int id,
        [FromServices] IJoinEventService joinEventService
    )
    {
        try
        {
            var joinEvent = await joinEventService.GetAsync(id);
            return joinEvent == null ? TypedResults.NotFound() : TypedResults.Ok(joinEvent);
        }
        catch (Exception)
        {
            return TypedResults.BadRequest(
                DefaultErrorMessage
            );
        }
    }
    
    private static async Task<Results<Ok<ICollection<Cinema>>, NotFound, BadRequest<string>>> GetCinemas(
        [FromServices] IKinoJoinService kinoJoinService)
    {
        try
        {
            var cinemas = await kinoJoinService.GetAllCinemas();
            return cinemas.Any() ? TypedResults.Ok(cinemas) : TypedResults.NotFound();
        }
        catch (Exception)
        {
            return TypedResults.BadRequest(
                DefaultErrorMessage
            );
        }
    }
    
    private static async Task<Results<Ok<ICollection<Movie>>, NotFound, BadRequest<string>>> GetMovies(
        [FromServices] IKinoJoinService kinoJoinService)
    {
        try
        {
            var movies = await kinoJoinService.GetAllMovies();
            return movies.Any() ? TypedResults.Ok(movies) : TypedResults.NotFound();
        }
        catch (Exception)
        {
            return TypedResults.BadRequest(
                DefaultErrorMessage
            );
        }
    }
    
    private static async Task<Results<Ok<ICollection<Genre>>, NotFound, BadRequest<string>>> GetGenres(
        [FromServices] IKinoJoinService kinoJoinService)
    {
        try
        {
            var genres = await kinoJoinService.GetAllGenres();
            return genres.Any() ? TypedResults.Ok(genres) : TypedResults.NotFound();
        }
        catch (Exception)
        {
            return TypedResults.BadRequest(
                DefaultErrorMessage
            );
        }
    }

    //Delete participant
    public static async Task<Results<Ok, NotFound>> DeleteParticipant(
        [FromRoute] int eventId,
        [FromRoute] int participantId,
        [FromServices] IJoinEventService joinEventService
    )
    {
        try
        {
            await joinEventService.DeleteParticipantAsync(eventId, participantId);
            return TypedResults.Ok();
        }
        catch (Exception e)
        {
            return TypedResults.NotFound();
        }
    }
}