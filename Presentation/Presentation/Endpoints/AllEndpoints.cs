using Application.DTO;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Database;
using Microsoft.AspNetCore.Mvc;
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
        app.MapGet(
            "/event/{id}",
            async (int id) =>
            {
                using var scope = app.Services.CreateScope();

                var result = await scope
                    .ServiceProvider.GetRequiredService<KinoContext>()
                    .JoinEvents.Select(e => new JoinEvent
                    {
                        Id = e.Id,
                        Title = e.Title,
                        Description = e.Description,
                        Deadline = e.Deadline,
                        Host = e.Host,
                        ChosenShowtimeId = e.ChosenShowtimeId,
                        Participants = e
                            .Participants.Select(p => new Participant
                            {
                                Id = p.Id,
                                Nickname = p.Nickname,
                                Email = p.Email,
                                Note = p.Note,
                                AuthId = p.AuthId,
                                VotedFor = p
                                    .VotedFor.Select(v => new ParticipantVote
                                    {
                                        ParticipantId = v.ParticipantId,
                                        ShowtimeId = v.ShowtimeId,
                                        VoteIndex = v.VoteIndex
                                    })
                                    .ToList()
                            })
                            .ToList(),
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
                                VersionTag = s.VersionTag
                            })
                            .ToList(),
                        SelectOptions = e
                            .SelectOptions.Select(so => new SelectOption
                            {
                                Id = so.Id,
                                VoteOption = so.VoteOption,
                                Color = so.Color
                            })
                            .ToList()
                    })
                    .FirstOrDefaultAsync(e => e.Id == id);
                return Results.Ok(result);
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
