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
        var group = app.MapGroup("api/events");

        group.MapPut("", UpsertJoinEvent);

        group.MapGet("", GetJoinEvents);
        group.MapGet("{id}", GetJoinEvent);
    }

    //Result<> is a union type, that can be all the different responses we can return, so it is easier to test.
    private static async Task<Results<Ok<int>, BadRequest<string>>> UpsertJoinEvent(
        [FromBody] UpsertJoinEventDto upsertJoinEventDto,
        [FromServices] IJoinEventService joinEventService
    )
    {
        try
        {
            var result = await joinEventService.UpsertJoinEventAsync(upsertJoinEventDto);
            return TypedResults.Ok(result);
        }
        catch (Exception e)
        {
            //Todo tror ikke man må exponere hele exception til klienten.
            return TypedResults.BadRequest(e.Message);
        }
    }

    private static async Task<Results<Ok<List<JoinEvent>>, NotFound>> GetJoinEvents(
        [FromServices] IJoinEventService joinEventService
    )
    {
        var joinEvents = await joinEventService.GetAllAsync();
        if (joinEvents.Count == 0)
            return TypedResults.NotFound();
        return TypedResults.Ok(joinEvents);
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
