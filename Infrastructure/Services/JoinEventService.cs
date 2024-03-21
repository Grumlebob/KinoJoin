using System.Linq.Expressions;
using Infrastructure.Persistence;

namespace Infrastructure.Services;

public class JoinEventService(KinoContext context) : IJoinEventService
{
    //optimized db calls
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

    //optimized db calls
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

    //OPTIMIZED DB CALLS
    public async Task DeleteParticipantAsync(int eventId, int participantId)
    {
        // Find the participant directly without loading the entire JoinEvent and Participants
        var participant = await context.Participants.FirstOrDefaultAsync(p =>
            p.Id == participantId && p.JoinEventId == eventId
        );

        // If the participant exists, remove it
        if (participant != null)
        {
            context.Participants.Remove(participant);
            await context.SaveChangesAsync();
        }
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
            await context.Movies!.UpsertRange(movies).RunAsync();

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
        await HandleStaticKinoData(joinEvent); //mangler optimize playtimes
        await HandleMovies(joinEvent);
        await HandleHost(joinEvent);
        await HandleShowtimes(joinEvent);
        await HandleSelectOptions(joinEvent); //mangler optimization
        await HandleDefaultSelectOptions(joinEvent);
        var addedId = await HandleJoinEvent(joinEvent);
        await HandleParticipants(joinEvent); //mangler optimization

        return addedId;
    }

    //Optimized DB calls except playtimes.
    /// <summary>
    ///  Handles independent data that came from Kino.dk, like cinemas, movies, playtimes...
    /// </summary>
    private async Task HandleStaticKinoData(JoinEvent joinEvent)
    {
        var cinemaIds = joinEvent.Showtimes.Select(st => st.Cinema.Id).Distinct().ToList();
        var playtimeStartTimes = joinEvent.Showtimes.Select(st => st.Playtime.StartTime).Distinct();
        var versionTypes = joinEvent.Showtimes.Select(st => st.VersionTag.Type).Distinct().ToList();
        var roomIds = joinEvent.Showtimes.Select(st => st.Room.Id).Distinct().ToList();
        var all = await context.Playtimes.ToListAsync();
        var existingCinemas = await context
            .Cinemas.Where(c => cinemaIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id);
        var existingPlaytimes = await context
            .Playtimes.Where(p => playtimeStartTimes.Contains(p.StartTime))
            .ToDictionaryAsync(p => p.StartTime);
        var existingVersionTags = await context
            .Versions.Where(v => versionTypes.Contains(v.Type))
            .ToDictionaryAsync(v => v.Type);
        var existingRooms = await context
            .Rooms.Where(r => roomIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id);

        foreach (var showtime in joinEvent.Showtimes)
        {
            // Handle Cinema
            if (!existingCinemas.TryGetValue(showtime.Cinema.Id, out var existingCinema))
            {
                existingCinema = new Cinema
                {
                    Id = showtime.Cinema.Id,
                    Name = showtime.Cinema.Name
                };
                context.Cinemas.Add(existingCinema);
                existingCinemas[existingCinema.Id] = existingCinema;
            }

            showtime.Cinema = existingCinema;

            //For some reason context.playtimes.Where doesn't work. It returns null.
            // Handle Playtime
            if (
                !existingPlaytimes.TryGetValue(
                    showtime.Playtime.StartTime,
                    out var existingPlaytime
                )
            )
            {
                existingPlaytime = new Playtime { StartTime = showtime.Playtime.StartTime };
                context.Playtimes.Add(existingPlaytime);
                existingPlaytimes[existingPlaytime.StartTime] = existingPlaytime;
            }
            showtime.Playtime = existingPlaytime;

            //// Handle Playtime
            //var existingPlaytime = await context.Playtimes.FirstOrDefaultAsync(p =>
            //    p.StartTime == showtime.Playtime.StartTime
            //);
            //if (existingPlaytime != null)
            //{
            //    context.Playtimes.Attach(existingPlaytime);
            //    showtime.Playtime = existingPlaytime;
            //}
            //else
            //{
            //    context.Playtimes.Add(showtime.Playtime);
            //}

            // Handle VersionTag
            if (
                !existingVersionTags.TryGetValue(
                    showtime.VersionTag.Type,
                    out var existingVersionTag
                )
            )
            {
                existingVersionTag = new VersionTag { Type = showtime.VersionTag.Type };
                context.Versions.Add(existingVersionTag);
                existingVersionTags[existingVersionTag.Type] = existingVersionTag;
            }

            showtime.VersionTag = existingVersionTag;

            // Handle Room
            if (!existingRooms.TryGetValue(showtime.Room.Id, out var existingRoom))
            {
                existingRoom = new Room { Id = showtime.Room.Id, Name = showtime.Room.Name };
                context.Rooms.Add(existingRoom);
                existingRooms[existingRoom.Id] = existingRoom;
            }

            showtime.Room = existingRoom;
        }

        await context.SaveChangesAsync();
    }

    //OPTIMIZED DB CALLS
    private async Task HandleMovies(JoinEvent joinEvent)
    {
        // Collect all distinct movie IDs from the joinEvent's showtimes
        var movieIds = joinEvent.Showtimes.Select(st => st.Movie.Id).Distinct().ToList();

        // Fetch all existing movies in a single query
        var existingMovies = await context.Movies.Where(m => movieIds.Contains(m.Id)).ToListAsync();

        var existingAgeRatings = await context.AgeRatings.ToListAsync();

        foreach (var movie in joinEvent.Showtimes.Select(st => st.Movie).Distinct())
        {
            if (movie == null)
                continue;
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
                var existingAgeRating = existingAgeRatings.FirstOrDefault(ar =>
                    ar.Censorship == movie.AgeRating?.Censorship
                );
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
                var existingAgeRating = existingAgeRatings.FirstOrDefault(ar =>
                    ar.Censorship == movie.AgeRating?.Censorship
                );
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

        var existingShowtimes = await context
            .Showtimes.Where(s => showtimeIds.Contains(s.Id))
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
        await context
            .SelectOptions.UpsertRange(joinEvent.SelectOptions)
            .On(s => new { s.VoteOption, s.Color })
            .RunAsync();

        var allSelectOptions = await context.SelectOptions.ToListAsync();

        var upsertedSelectOptions = allSelectOptions
            .Where(so =>
                joinEvent.SelectOptions.Any(eventSo =>
                    eventSo.VoteOption == so.VoteOption && eventSo.Color == so.Color
                )
            )
            .ToList();

        joinEvent.SelectOptions = upsertedSelectOptions;
    }

    //OPTIMIZED DB CALLS
    //It will first check the joinEvent.SelectOptions, as that list has been processed in previous method and includes references to DB entities
    private async Task HandleDefaultSelectOptions(JoinEvent joinEvent)
    {
        var defaultSelectOption = joinEvent.DefaultSelectOption;

        // Check if the DefaultSelectOption is already in the prefetched list
        var existingDefaultSelectOption = joinEvent.SelectOptions.FirstOrDefault(so =>
            so.VoteOption == defaultSelectOption.VoteOption && so.Color == defaultSelectOption.Color
        );

        if (existingDefaultSelectOption != null)
        {
            // Use the existing option
            joinEvent.DefaultSelectOption = existingDefaultSelectOption;
            joinEvent.DefaultSelectOptionId = existingDefaultSelectOption.Id;
        }
        else
        {
            // Check if it exists in the database (in case it's not in the prefetched list)
            existingDefaultSelectOption = await context.SelectOptions.FirstOrDefaultAsync(so =>
                so.VoteOption == defaultSelectOption.VoteOption
                && so.Color == defaultSelectOption.Color
            );

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
            var allSelectoptions = await context.SelectOptions.ToListAsync();
            foreach (
                var vote in joinEvent.Participants.SelectMany(participant => participant.VotedFor)
            )
            {
                var selectOption = allSelectoptions.FirstOrDefault(so =>
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
