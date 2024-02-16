using Application.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Endpoints;

public static class MonkeyEndpoints
{
    public static void MapMonkeyEndpoints(this WebApplication app)
    {
        app.MapGet("/monkeys/{id}", ([FromServices] IMonkeyService service, int id) =>
        {
            var result = service.Get(id);
            if (result is null) return Results.NotFound();
            return Results.Ok(result);
        });
    }
}