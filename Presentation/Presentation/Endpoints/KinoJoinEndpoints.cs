using Domain;

namespace Presentation.Endpoints;

//ICarterModule gives access to useful extension methods, such as authorize all endpoints
public class KinoJoinEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var eventGroup = app.MapGroup("api/events");
        eventGroup.MapGet("{id}", GetJoinEventAsync);
        eventGroup.MapGet("host/{hostId}", GetJoinEventsByHostIdAsync);
        eventGroup.MapPut("", UpsertJoinEventAsync);
        eventGroup.MapDelete(
            "{joinEventId}/participants/{participantId}",
            MakeParticipantNotExistAsync
        );

        var kinoDataGroup = app.MapGroup("api/kino-data");
        kinoDataGroup.MapGet("cinemas", GetCinemasAsync);
        kinoDataGroup.MapGet("movies", GetMoviesAsync);
        kinoDataGroup.MapGet("genres", GetGenresAsync);
        kinoDataGroup.MapPost("update-all/{fromId}/{toId}", UpdateAllBaseDataFromKinoDkAsync);
    }

    private static async Task<
        Results<NotFound, Ok<JoinEvent>, BadRequest<string>>
    > GetJoinEventAsync([FromRoute] int id, [FromServices] IKinoJoinDbService kinoJoinDbService)
    {
        var joinEvent = await kinoJoinDbService.GetJoinEventAsync(id);
        return joinEvent == null ? TypedResults.NotFound() : TypedResults.Ok(joinEvent);
    }

    private static async Task<
        Results<Ok<List<JoinEvent>>, NotFound, BadRequest<string>>
    > GetJoinEventsByHostIdAsync(
        [FromServices] IKinoJoinDbService kinoJoinDbService,
        [FromRoute] string hostId
    )
    {
        var joinEvents = await kinoJoinDbService.GetAllJoinEventsAsync(j => j.HostId == hostId);
        return TypedResults.Ok(joinEvents);
    }

    private static async Task<Results<Ok<int>, BadRequest<string>>> UpsertJoinEventAsync(
        [FromBody] JoinEvent joinEvent,
        [FromServices] IKinoJoinDbService kinoJoinDbService
    )
    {
        var validator = new DataAnnotationsValidator.DataAnnotationsValidator();
        var validationResults = new List<ValidationResult>();
        validator.TryValidateObjectRecursive(joinEvent, validationResults);

        //If there are any validation errors, they will be stored in the validationResults list
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
        Results<Ok, NotFound, BadRequest<string>>
    > MakeParticipantNotExistAsync(
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

    private static async Task<
        Results<Ok<ICollection<Cinema>>, NotFound, BadRequest<string>>
    > GetCinemasAsync([FromServices] IKinoJoinDbService kinoKinoJoinService)
    {
        var cinemas = await kinoKinoJoinService.GetAllCinemasAsync();
        return TypedResults.Ok(cinemas);
    }

    private static async Task<
        Results<Ok<ICollection<Movie>>, NotFound, BadRequest<string>>
    > GetMoviesAsync([FromServices] IKinoJoinDbService kinoKinoJoinService)
    {
        var movies = await kinoKinoJoinService.GetAllMoviesAsync();
        return TypedResults.Ok(movies);
    }

    private static async Task<
        Results<Ok<ICollection<Genre>>, NotFound, BadRequest<string>>
    > GetGenresAsync([FromServices] IKinoJoinDbService kinoKinoJoinService)
    {
        var genres = await kinoKinoJoinService.GetAllGenresAsync();
        return TypedResults.Ok(genres);
    }

    private static async Task<Results<Ok, BadRequest<string>>> UpdateAllBaseDataFromKinoDkAsync(
        [FromServices] IFetchNewestKinoDkDataService service,
        [FromRoute] int fromId,
        [FromRoute] int toId
    )
    {
        await service.UpdateBaseDataFromKinoDk(fromId, toId);
        return TypedResults.Ok();
    }
}
