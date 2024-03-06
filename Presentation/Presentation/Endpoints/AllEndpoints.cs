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
                                        Participant = v.Participant,
                                        ShowtimeId = v.ShowtimeId,
                                        Showtime = v.Showtime,
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
                                VersionTag = s.VersionTag,
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
        /*
        app.MapPut(
            "/participate/{eventId}",
            async (int eventId, [FromBody] Participant p) =>
            {
                await using var context = app
                    .Services.CreateScope()
                    .ServiceProvider.GetRequiredService<KinoContext>();
                var cinemaIds = p.VotedFor.Select(vote => vote.Showtime.Cinema.Id).Distinct();
                foreach (var cinemaId in cinemaIds)
                {
                    var existingCinema = await context.Cinemas.FindAsync(cinemaId);
                    if (existingCinema == null)
                    {
                        // This should only happen if you're sure you want to add new Cinemas
                        var cinemaName = p
                            .VotedFor.FirstOrDefault(vote => vote.Showtime.Cinema.Id == cinemaId)
                            ?.Showtime.Cinema.Name;
                        context.Cinemas.Add(new Cinema { Id = cinemaId, Name = cinemaName });
                    }
                    else
                    {
                        // Attach existing cinemas to each showtime
                        foreach (
                            var participantVote in p.VotedFor.Where(vote =>
                                vote.Showtime.Cinema.Id == cinemaId
                            )
                        )
                        {
                            participantVote.Showtime.Cinema = existingCinema;
                        }
                    }
                }

                foreach (var participantVote in p.VotedFor)
                {
                    var movieIds = p.VotedFor.Select(vote => vote.Showtime.Movie.Id).Distinct();
                    foreach (var movieId in movieIds)
                    {
                        var existingMovie = await context.Movies.FindAsync(movieId);
                        if (existingMovie == null)
                        {
                            //find the correct movie in st
                            var movie = p
                                .VotedFor.FirstOrDefault(vote => vote.Showtime.Movie.Id == movieId)
                                ?.Showtime.Movie;

                            // This should only happen if you're sure you want to add new Movies
                            context.Movies.Add(
                                new Movie
                                {
                                    Id = movieId, //insert all properties
                                    AgeRating = movie.AgeRating,
                                    Duration = movie.Duration,
                                    ImageUrl = movie.ImageUrl,
                                    Title = movie.Title,
                                    PremiereDate = movie.PremiereDate,
                                    KinoURL = movie.KinoURL
                                }
                            ); // Specify other properties
                        }
                        else
                        {
                            // Attach existing movies to each showtime
                            foreach (
                                var pVote in p.VotedFor.Where(vote =>
                                    vote.Showtime.Movie.Id == movieId
                                )
                            )
                            {
                                pVote.Showtime.Movie = existingMovie;
                            }
                        }
                    }

                    var existingRoom = await context.Rooms.FindAsync(
                        participantVote.Showtime.Room.Id
                    );
                    if (existingRoom != null)
                    {
                        context.Rooms.Attach(existingRoom);
                        participantVote.Showtime.Room = existingRoom;
                    }
                    else
                    {
                        context.Rooms.Add(participantVote.Showtime.Room);
                    }

                    var existingVersionTag = await context.Versions.FirstOrDefaultAsync(v =>
                        v.Type == participantVote.Showtime.VersionTag.Type
                    );
                    if (existingVersionTag != null)
                    {
                        Console.WriteLine("Existing version tag: " + existingVersionTag.Type);
                        context.Versions.Attach(existingVersionTag);
                        participantVote.Showtime.VersionTag = existingVersionTag;
                    }
                    else
                    {
                        context.Versions.Add(participantVote.Showtime.VersionTag);
                    }

                    // Handle Playtime
                    var existingPlaytime = await context.Playtimes.FirstOrDefaultAsync(p =>
                        p.StartTime == participantVote.Showtime.Playtime.StartTime
                    );
                    if (existingPlaytime != null)
                    {
                        context.Playtimes.Attach(existingPlaytime);
                        participantVote.Showtime.Playtime = existingPlaytime;
                    }
                    else
                    {
                        context.Playtimes.Add(participantVote.Showtime.Playtime);
                    }

                    var existingShowtime = await context.Showtimes.FindAsync(
                        participantVote.Showtime.Id
                    );
                    if (existingShowtime != null)
                    {
                        context.Showtimes.Attach(existingShowtime);
                        participantVote.Showtime.Id = existingShowtime.Id;
                    }
                    else
                    {
                        //add new showtime, with only the Ids of the related entities
                        var newShowtime = new Showtime
                        {
                            Id = participantVote.Showtime.Id,
                            MovieId = participantVote.Showtime.Movie.Id,
                            CinemaId = participantVote.Showtime.Cinema.Id,
                            PlaytimeId = participantVote.Showtime.Playtime.Id,
                            VersionTagId = participantVote.Showtime.VersionTag.Id,
                            RoomId = participantVote.Showtime.Room.Id
                        };

                        context.Showtimes.Add(newShowtime);
                    }
                }

                var ShowtimesToAttach = new List<Showtime>();
                foreach (var participantVote in p.VotedFor)
                {
                    var existingShowtime = await context.Showtimes.FindAsync(
                        participantVote.Showtime.Id
                    );
                    if (existingShowtime != null)
                    {
                        ShowtimesToAttach.Add(existingShowtime);
                    }
                    else
                    {
                        ShowtimesToAttach.Add(participantVote.Showtime);
                    }
                }

                var participant = new Participant
                {
                    AuthId = p.AuthId,
                    Id = p.Id,
                    JoinEventId = p.JoinEventId,
                    Nickname = p.Nickname,
                    Email = p.Email,
                    Note = p.Note,
                    VotedFor = p
                        .VotedFor.Select(v => new ParticipantVote()
                        {
                            Participant = p,
                            Showtime = ShowtimesToAttach.First(s => s.Id == v.ShowtimeId),
                            VoteIndex = v.VoteIndex
                        })
                        .ToList()
                };

                await context.Participants.AddAsync(participant);

                await context.SaveChangesAsync();
                var joinEvent = await context
                    .JoinEvents.Include(e => e.Participants)
                    .FirstOrDefaultAsync(e => e.Id == eventId);
                if (
                    joinEvent is { Participants: not null }
                    && joinEvent.Participants.Exists(eP => eP.Id == p.Id)
                )
                {
                    joinEvent.Participants.Add(p);
                }

                await context.SaveChangesAsync();
                return Results.Ok(participant.Id);
            }
        );
    */
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
