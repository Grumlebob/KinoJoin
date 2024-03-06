using Application.DTO;
using Application.Interfaces;
using Carter;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Endpoints;

public class KinoEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(""); //Tilføj prefix til alle endpoints såsom /.../Events/
        
        group.MapPut("/putJoinEvent", UpsertJoinEvent); //todo skal ikke hede "put" skal bare være /Events/ med forskellige metoder og input
    }

    //Result<> is a union type, that can be all the different responses we can return, so it is easier to test.
    public static async Task<Results<Ok<int>,BadRequest<string>>> UpsertJoinEvent([FromBody] UpsertJoinEventDto UpsertJoinEventDto,
        [FromServices] IJoinEventService service)
    {
        try
        {
            var result = await service.PutAsync(UpsertJoinEventDto);
            return TypedResults.Ok(result);
        }
        catch (Exception e)
        {
            return TypedResults.BadRequest(e.Message);
        }
    }
}