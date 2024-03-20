﻿using System.Linq.Expressions;
using Infrastructure.Persistence;

namespace Infrastructure.Services;

public class JoinEventService(KinoContext context) : IJoinEventService
{
    public async Task<JoinEvent?> GetAsync(int id)
    {
        var result = await context
            .JoinEvents.AsNoTracking()
            .Include(j => j.Showtimes)
            .ThenInclude(s => s.Movie)
            .ThenInclude(m => m.AgeRating)
            .Include(j => j.Showtimes)
            .ThenInclude(s => s.Cinema)
            .Include(j => j.Showtimes)
            .ThenInclude(s => s.Playtime)
            .Include(j => j.Showtimes)
            .ThenInclude(s => s.VersionTag)
            .Include(j => j.Showtimes)
            .ThenInclude(s => s.Room)
            .Include(j => j.Participants)!
            .ThenInclude(p => p.VotedFor)
            .ThenInclude(pv => pv.SelectedOption)
            .Include(j => j.SelectOptions)
            .Include(j => j.DefaultSelectOption)
            .Include(j => j.Host)
            .FirstOrDefaultAsync(j => j.Id == id);
        return result;
    }

    public async Task<List<JoinEvent>> GetAllAsync(Expression<Func<JoinEvent, bool>>? filter = null)
    {
        IQueryable<JoinEvent> query = context.JoinEvents.AsNoTracking();

        if (filter != null)
        {
            query = query.Where(filter);
        }

        var joinEvents = await query
            .Include(j => j.Showtimes)
            .ThenInclude(s => s.Movie)
            .ThenInclude(m => m.AgeRating)
            .Include(j => j.Showtimes)
            .ThenInclude(s => s.Cinema)
            .Include(j => j.Showtimes)
            .ThenInclude(s => s.Playtime)
            .Include(j => j.Showtimes)
            .ThenInclude(s => s.VersionTag)
            .Include(j => j.Showtimes)
            .ThenInclude(s => s.Room)
            .Include(j => j.Participants)!
            .ThenInclude(p => p.VotedFor)
            .ThenInclude(pv => pv.SelectedOption)
            .Include(j => j.SelectOptions)
            .Include(j => j.DefaultSelectOption)
            .Include(j => j.Host)
            .ToListAsync();

        return joinEvents;
    }

    public async Task DeleteParticipantAsync(int eventId, int participantId)
    {
        var joinEvent = await context
            .JoinEvents.Include(e => e.Participants)
            .FirstOrDefaultAsync(e => e.Id == eventId);
        if (
            joinEvent is { Participants: not null }
            && joinEvent.Participants.Exists(eP => eP.Id == participantId)
        )
        {
            joinEvent.Participants.Remove(joinEvent.Participants.First(p => p.Id == participantId));
        }
        await context.SaveChangesAsync();
    }

    /*
     * The overall goal is to save the entire workpage of a JoinEvent, regardless of creating or filling
     *
     * Stages:
     * 1. We need to upsert entities that needs to exist in the database before JoinEvent is added.
     * 2. Upon program startup, the DB is loaded with Kino's data.
     * 2.1 We try to upsert the joinEvent using the preloaded data in the database.
     * 2.2 In unlikely scenario it fails, Kino have updated their database with new data, which will be added.
     * It is then reattempted to upsert the joinEvent.
     */
    public async Task<int> UpsertJoinEventAsync(JoinEvent updatedJoinEvent)
    {
        var isUpdate = updatedJoinEvent.Id != 0;

        if (isUpdate)
        {
            return await UpdateJoinEventAsync(updatedJoinEvent);
        }

        //Attempt without preloaded data
        try
        {
            var newId = await InsertJoinEventAsync(updatedJoinEvent);
            return newId;
        }
        catch (Exception)
        {
            //Add missing entities and try again.
            var movies = updatedJoinEvent.Showtimes.Select(st => st.Movie).DistinctBy(m => m.Id);
            await context.Movies.UpsertRange(movies).RunAsync();

            var cinemas = updatedJoinEvent.Showtimes.Select(st => st.Cinema).DistinctBy(c => c.Id);
            await context.Cinemas.UpsertRange(cinemas).RunAsync();

            var versions = updatedJoinEvent
                .Showtimes.Select(st => st.VersionTag)
                .DistinctBy(v => v.Type);
            await context.Versions.UpsertRange(versions).On(v => v.Type).RunAsync();
            var newId = await InsertJoinEventAsync(updatedJoinEvent);
            return newId;
        }
    }
    
    private async Task<int> UpdateJoinEventAsync(JoinEvent updatedJoinEvent)
    {
        await context.JoinEvents.ExecuteUpdateAsync(setters =>
            setters
                .SetProperty(b => b.Title, updatedJoinEvent.Title)
                .SetProperty(b => b.Description, updatedJoinEvent.Description)
                .SetProperty(b => b.ChosenShowtimeId, updatedJoinEvent.ChosenShowtimeId)
                .SetProperty(b => b.Deadline, updatedJoinEvent.Deadline)
        );

        context.ChangeTracker.Clear();
        // Fetch existing participants from the database
        var existingParticipants = await context
            .Participants.Where(p => p.JoinEventId == updatedJoinEvent.Id)
            .ToListAsync();

        // Update existing participants
        foreach (var existingParticipant in existingParticipants)
        {
            var updatedParticipant = updatedJoinEvent.Participants?.FirstOrDefault(p =>
                p.Id == existingParticipant.Id
            );

            if (updatedParticipant != null)
            {
                context.Entry(existingParticipant).CurrentValues.SetValues(updatedParticipant);
            }
        }

        // Identify and add new participants
        if (updatedJoinEvent.Participants != null)
        {
            var newParticipants = updatedJoinEvent.Participants.Where(p => p.Id == 0).ToList();

            if (newParticipants.Count != 0)
            {
                foreach (var newParticipant in newParticipants)
                {
                    foreach (var option in newParticipant.VotedFor)
                    {
                        option.SelectedOptionId = option.SelectedOption.Id;
                        option.SelectedOption = null!; //don't track
                    }

                    newParticipant.JoinEventId = updatedJoinEvent.Id;
                    await context.Participants.AddAsync(newParticipant);
                }

                await context.Participants.AddRangeAsync(newParticipants);
            }
        }

        await context.SaveChangesAsync();

        return updatedJoinEvent.Id;
    }

    private async Task<int> InsertJoinEventAsync(JoinEvent joinEvent)
    {
        context.ChangeTracker.Clear();

        //---The Order matters, certain entities need to be added before others---
        await HandleCinemas(joinEvent);
        await HandleMovies(joinEvent);
        await HandleHost(joinEvent);
        await HandleShowtimes(joinEvent);
        await HandleSelectOptions(joinEvent);
        await HandleDefaultSelectOptions(joinEvent);
        var addedId = await HandleJoinEvent(joinEvent);
        await HandleParticipants(joinEvent);

        return addedId;
    }

    private async Task HandleCinemas(JoinEvent joinEvent)
    {
        foreach (var st in joinEvent.Showtimes)
        {
            var cinemaIds = joinEvent.Showtimes.Select(showtime => showtime.Cinema.Id).Distinct();
            foreach (var cinemaId in cinemaIds)
            {
                var existingCinema = await context.Cinemas.FindAsync(cinemaId);
                if (existingCinema == null)
                {
                    // This should only happen if you're sure you want to add new Cinemas
                    var cinemaName = joinEvent
                        .Showtimes.FirstOrDefault(showtime => showtime.Cinema.Id == cinemaId)
                        ?.Cinema.Name;
                    context.Cinemas.Add(new Cinema { Id = cinemaId, Name = cinemaName! });
                }
                else
                {
                    // Attach existing cinemas to each showtime
                    foreach (
                        var showtime in joinEvent.Showtimes.Where(showtime =>
                            showtime.Cinema.Id == cinemaId
                        )
                    )
                    {
                        showtime.Cinema = existingCinema;
                    }
                }
            }

            // Handle Playtime
            var existingPlaytime = await context.Playtimes.FirstOrDefaultAsync(p =>
                p.StartTime == st.Playtime.StartTime
            );
            if (existingPlaytime != null)
            {
                context.Playtimes.Attach(existingPlaytime);
                st.Playtime = existingPlaytime;
            }
            else
            {
                context.Playtimes.Add(st.Playtime);
            }

            // Handle VersionTag
            var existingVersionTag = await context.Versions.FirstOrDefaultAsync(v =>
                v.Type == st.VersionTag.Type
            );
            if (existingVersionTag != null)
            {
                context.Versions.Attach(existingVersionTag);
                st.VersionTag = existingVersionTag;
            }
            else
            {
                context.Versions.Add(st.VersionTag);
            }

            // Handle Sal
            var existingSal = await context.Rooms.FindAsync(st.Room.Id);
            if (existingSal != null)
            {
                context.Rooms.Attach(existingSal);
                st.Room = existingSal;
            }
            else
            {
                context.Rooms.Add(st.Room);
            }

            await context.SaveChangesAsync();
        }
    }

    //OPTIMIZED DB CALLS
    private async Task HandleMovies(JoinEvent joinEvent)
    {
        // Collect all distinct movie IDs from the joinEvent's showtimes
        var movieIds = joinEvent.Showtimes.Select(st => st.Movie.Id).Distinct().ToList();

        // Fetch all existing movies in a single query
        var existingMovies = await context.Movies
            .Where(m => movieIds.Contains(m.Id))
            .ToListAsync();

        var existingAgeRatings = await context.AgeRatings.ToListAsync();

        foreach (var movie in joinEvent.Showtimes.Select(st => st.Movie).Distinct())
        {
            var existingMovie = existingMovies.FirstOrDefault(m => m.Id == movie.Id);
        
            if (existingMovie != null)
            {
                // Update the existing movie details
                existingMovie.KinoUrl = movie.KinoUrl;
                existingMovie.Duration = movie.Duration;
                existingMovie.ImageUrl = movie.ImageUrl;
                existingMovie.Title = movie.Title;
                existingMovie.PremiereDate = movie.PremiereDate;

                // Update age rating
                var existingAgeRating = existingAgeRatings.FirstOrDefault(ar => ar.Censorship == movie.AgeRating.Censorship);
                if (existingAgeRating != null)
                {
                    existingMovie.AgeRating = existingAgeRating;
                }
            }
            else
            {
                // Add the new movie
                context.Movies.Add(movie);

                // Update age rating
                var existingAgeRating = existingAgeRatings.FirstOrDefault(ar => ar.Censorship == movie.AgeRating.Censorship);
                if (existingAgeRating != null)
                {
                    movie.AgeRating = existingAgeRating;
                }
            }
        }

        await context.SaveChangesAsync();
    }


    //OPTIMIZED DB CALLS
    private async Task HandleHost(JoinEvent joinEvent)
    {
        var existingHost = await context.Hosts.FindAsync(joinEvent.Host.AuthId);
        if (existingHost == null)
        {
            context.Hosts.Add(joinEvent.Host);
        }
        else
        {
            joinEvent.Host = existingHost;
        }
        await context.SaveChangesAsync();
    }

    //OPTIMIZED DB CALLS
    private async Task HandleShowtimes(JoinEvent joinEvent)
    {
        var showtimeIds = joinEvent.Showtimes.Select(s => s.Id).ToList();

        var existingShowtimes = await context.Showtimes
            .Where(s => showtimeIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id);

        var newShowtimesToBeAdded = new List<Showtime>();
        var showtimesWithEfCoreTracking = new List<Showtime>();

        foreach (var showtime in joinEvent.Showtimes)
        {
            if (existingShowtimes.TryGetValue(showtime.Id, out var existingShowtime))
            {
                // If the Showtime exists, add the existing Showtime to the updated list
                showtimesWithEfCoreTracking.Add(existingShowtime);
            }
            else
            {
                // If the Showtime does not exist, prepare it for batch insert
                var newShowtime = new Showtime
                {
                    Id = showtime.Id,
                    MovieId = showtime.Movie.Id,
                    CinemaId = showtime.Cinema.Id,
                    PlaytimeId = showtime.Playtime.Id,
                    VersionTagId = showtime.VersionTag.Id,
                    RoomId = showtime.Room.Id
                };
                newShowtimesToBeAdded.Add(newShowtime);
                showtimesWithEfCoreTracking.Add(newShowtime);
            }
        }

        if (newShowtimesToBeAdded.Any())
        {
            await context.Showtimes.AddRangeAsync(newShowtimesToBeAdded);
            await context.SaveChangesAsync();
        }

        // Update the joinEvent.Showtimes with the updated list
        joinEvent.Showtimes = showtimesWithEfCoreTracking;
    }

    private async Task HandleSelectOptions(JoinEvent joinEvent)
    {
        var selectOptionsToAttach = new List<SelectOption>();
        foreach (var selectOption in joinEvent.SelectOptions)
        {
            // Find existing based on color and voteOption
            var existingSelectOption = await context.SelectOptions.FirstOrDefaultAsync(so =>
                so.VoteOption == selectOption.VoteOption && so.Color == selectOption.Color
            );

            if (existingSelectOption != null)
            {
                // Existing option found, update the ID of the current option
                selectOption.Id = existingSelectOption.Id;
                selectOptionsToAttach.Add(existingSelectOption); // Attach the existing one
                context.Entry(existingSelectOption).CurrentValues.SetValues(selectOption); // Update the existing entity
            }
            else
            {
                // No existing option found, add a new one
                context.SelectOptions.Add(selectOption);
                selectOptionsToAttach.Add(selectOption); // Add the new one
            }

            await context.SaveChangesAsync();
        }

        joinEvent.SelectOptions = selectOptionsToAttach;
    }

    //OPTIMIZED DB CALLS
    //It will first check the joinEvent.SelectOptions, as that list has been processed in previous method and includes references to DB entities
    private async Task HandleDefaultSelectOptions(JoinEvent joinEvent)
    {
        var defaultSelectOption = joinEvent.DefaultSelectOption;
    
        // Check if the DefaultSelectOption is already in the prefetched list
        var existingDefaultSelectOption = joinEvent.SelectOptions
            .FirstOrDefault(so => so.VoteOption == defaultSelectOption.VoteOption
                                  && so.Color == defaultSelectOption.Color);

        if (existingDefaultSelectOption != null)
        {
            // Use the existing option
            joinEvent.DefaultSelectOption = existingDefaultSelectOption;
            joinEvent.DefaultSelectOptionId = existingDefaultSelectOption.Id;
        }
        else
        {
            // Check if it exists in the database (in case it's not in the prefetched list)
            existingDefaultSelectOption = await context.SelectOptions
                .FirstOrDefaultAsync(so => so.VoteOption == defaultSelectOption.VoteOption
                                           && so.Color == defaultSelectOption.Color);

            if (existingDefaultSelectOption != null)
            {
                // Use the existing option from the database
                joinEvent.DefaultSelectOption = existingDefaultSelectOption;
                joinEvent.DefaultSelectOptionId = existingDefaultSelectOption.Id;
            }
            else
            {
                // It doesn't exist in either the prefetched list or the database, so add it
                context.SelectOptions.Add(defaultSelectOption);
                await context.SaveChangesAsync();
                joinEvent.DefaultSelectOptionId = defaultSelectOption.Id;
            }
        }
    }

    //OPTIMIZED DB CALLS
    private async Task<int> HandleJoinEvent(JoinEvent joinEvent)
    {
        var newJoinEvent = new JoinEvent
        {
            Showtimes = joinEvent.Showtimes,
            Id = joinEvent.Id,
            Title = joinEvent.Title,
            Description = joinEvent.Description,
            Deadline = joinEvent.Deadline,
            HostId = joinEvent.Host.AuthId,
            ChosenShowtimeId = joinEvent.ChosenShowtimeId,
            DefaultSelectOptionId = joinEvent.DefaultSelectOptionId,
            SelectOptions = joinEvent.SelectOptions,
            Participants = null,
        };

        var addedId = context.JoinEvents.Add(newJoinEvent);
        await context.SaveChangesAsync();
        joinEvent.Id = addedId.Entity.Id;
        return addedId.Entity.Id;
    }

    private async Task HandleParticipants(JoinEvent joinEvent)
    {
        if (joinEvent.Participants != null)
        {
            foreach (
                var vote in joinEvent.Participants.SelectMany(participant => participant.VotedFor)
            )
            {
                var selectOption = await context.SelectOptions.FirstOrDefaultAsync(so =>
                    so.VoteOption == vote.SelectedOption.VoteOption
                    && so.Color == vote.SelectedOption.Color
                );

                if (selectOption != null)
                {
                    vote.SelectedOptionId = selectOption.Id; // Update the ID
                }
            }

            //Handle participants
            foreach (var p in joinEvent.Participants)
            {
                var participant = new Participant
                {
                    Id = p.Id,
                    AuthId = p.AuthId,
                    JoinEventId = joinEvent.Id,
                    Nickname = p.Nickname,
                    Email = p.Email,
                    Note = p.Note,
                    VotedFor = p
                        .VotedFor.Select(v => new ParticipantVote()
                        {
                            ParticipantId = p.Id,
                            ShowtimeId = joinEvent.Showtimes.First(s => s.Id == v.ShowtimeId).Id,
                            SelectedOptionId = v.SelectedOptionId
                        })
                        .ToList()
                };

                await context.Participants.AddAsync(participant);
            }
        }

        await context.SaveChangesAsync();
    }
}
