using Domain.Entities;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Presentation.Endpoints;

public static class AllEndpoints
{
    public static void MapKinoJoinEndpoints(this WebApplication app)
    {
        app.MapGet(
            "/health",
            async (HttpContext context) =>
            {
                await context.Response.WriteAsync("Healthy");
            }
        );
        app.MapGet(
            "events/{hostId}",
            async (string hostId) =>
            {
                using var scope = app.Services.CreateScope();

                await using var context = app
                    .Services.CreateScope()
                    .ServiceProvider.GetRequiredService<KinoContext>();

                var results = await context
                    .JoinEvents.Where(j => j.HostId == hostId)
                    .Select(e => new JoinEvent
                    {
                        Id = e.Id,
                        Title = e.Title,
                        Description = e.Description,
                        Deadline = e.Deadline,
                        Host = e.Host,
                        Showtimes = e
                            .Showtimes.Select(s => new Showtime
                            {
                                Id = s.Id,
                                Movie = new Movie()
                                {
                                    Id = s.MovieId,
                                    AgeRating = s.Movie.AgeRating,
                                    PremiereDate = s.Movie.PremiereDate,
                                    Title = s.Movie.Title,
                                    ImageUrl = s.Movie.ImageUrl,
                                    Duration = s.Movie.Duration,
                                    KinoURL = s.Movie.KinoURL
                                },
                                Cinema = s.Cinema,
                                Playtime = s.Playtime,
                                Room = s.Room,
                                VersionTag = s.VersionTag,
                            })
                            .ToList(),
                        Participants = e.Participants
                    })
                    .ToListAsync();

                //create empty participant lists if they are null
                foreach (var joinEvent in results)
                {
                    joinEvent.Participants ??= new List<Participant>();
                }

                return Results.Ok(results);
            }
        );
        app.MapDelete(
            "/deleteParticipant/{eventId}/{participantId}",
            async (int eventId, int participantId) =>
            {
                await using var context = app
                    .Services.CreateScope()
                    .ServiceProvider.GetRequiredService<KinoContext>();
                var joinEvent = await context
                    .JoinEvents.Include(e => e.Participants)
                    .FirstOrDefaultAsync(e => e.Id == eventId);
                if (
                    joinEvent is { Participants: not null }
                    && joinEvent.Participants.Exists(eP => eP.Id == participantId)
                )
                {
                    joinEvent.Participants.Remove(
                        joinEvent.Participants.First(p => p.Id == participantId)
                    );
                }

                await context.SaveChangesAsync();
                return Results.Ok();
            }
        );
    }
}
