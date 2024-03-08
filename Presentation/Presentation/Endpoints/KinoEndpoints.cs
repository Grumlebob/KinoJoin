using Application.DTO;
using Application.Interfaces;
using Carter;
using Domain.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Endpoints;

public class KinoEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/events"); //Tilføj prefix til alle endpoints såsom /.../Events/

        group.MapPut("", UpsertJoinEvent);
        group.MapGet("{id}", GetJoinEvent);
        group.MapGet("", GetJoinEvents);
    }

    //Result<> is a union type, that can be all the different responses we can return, so it is easier to test.
    private static async Task<Results<Ok<int>, BadRequest<string>>> UpsertJoinEvent(
        [FromBody] UpsertJoinEventDto upsertJoinEventDto,
        [FromServices] IJoinEventService joinEventService
    )
    {
        try
        {
            var result = await joinEventService.PutAsync(upsertJoinEventDto);
            return TypedResults.Ok(result);
        }
        catch (Exception e)
        {
            return TypedResults.BadRequest(e.Message);
        }
    }

    private static async Task<Results<Ok<List<JoinEvent>>, NotFound>> GetJoinEvents(
        [FromServices] IJoinEventService joinEventService
    )
    {
        return TypedResults.Ok(new List<JoinEvent>());
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
        catch (Exception e)
        {
            return TypedResults.BadRequest(e.Message);
        }
    }
}
