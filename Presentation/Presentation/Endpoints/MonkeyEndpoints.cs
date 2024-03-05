using Application.DTO;
using Application.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Endpoints;

public static class MonkeyEndpoints
{
    public static void MapMonkeyEndpoints(this WebApplication app)
    {
        app.MapGet(
            "/monkeys",
            async ([FromServices] IMonkeyService service) =>
            {
                var result = await service.GetAllAsync();
                return Results.Ok(result);
            }
        );

        app.MapGet(
            "/monkeys/{id}",
            async ([FromServices] IMonkeyService service, int id) =>
            {
                var result = await service.GetAsync(id);
                if (result is null)
                    return Results.NotFound();
                return Results.Ok(result);
            }
        );

        app.MapPost(
            "/monkeys",
            async ([FromServices] IMonkeyService service, [FromBody] CreateMonkeyDto monkey) =>
            {
                var newMonkey = await service.CreateAsync(monkey);
                return Results.Created($"/monkeys/{newMonkey.Id}", newMonkey);
            }
        );

        app.MapDelete(
            "/monkeys/{id}",
            async ([FromServices] IMonkeyService service, int id) =>
            {
                await service.DeleteAsync(id);
                return Results.NoContent();
            }
        );
    }
}
