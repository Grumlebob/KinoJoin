namespace Infrastructure.Services;

public class KinoJoinDbService(KinoContext context) : IKinoJoinDbService
{
    public async Task<ICollection<Cinema>> GetAllCinemasAsync()
    {
        return await context.Cinemas.AsNoTracking().ToListAsync();
    }

    public async Task<ICollection<Movie>> GetAllMoviesAsync()
    {
        return await context.Movies.AsNoTracking().ToListAsync();
    }

    public async Task<ICollection<Genre>> GetAllGenresAsync()
    {
        return await context.Genres.AsNoTracking().ToListAsync();
    }

    public async Task<JoinEvent?> GetJoinEventAsync(int id)
    {
        //Include all related entities, otherwise these would be set to null
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
            .Include(j => j.Participants)
            .ThenInclude(p => p.VotedFor)
            .ThenInclude(pv => pv.SelectedOption)
            .Include(j => j.SelectOptions)
            .Include(j => j.DefaultSelectOption)
            .Include(j => j.Host)
            .FirstOrDefaultAsync(j => j.Id == id);
        return result;
    }

    public async Task<List<JoinEvent>> GetAllJoinEventsAsync(
        Expression<Func<JoinEvent, bool>>? filter = null
    )
    {
        IQueryable<JoinEvent> query = context.JoinEvents.AsNoTracking();

        if (filter != null)
        {
            query = query.Where(filter);
        }

        //Include all related entities, otherwise these would be set to null
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
            .Include(j => j.Participants)
            .ThenInclude(p => p.VotedFor)
            .ThenInclude(pv => pv.SelectedOption)
            .Include(j => j.SelectOptions)
            .Include(j => j.DefaultSelectOption)
            .Include(j => j.Host)
            .ToListAsync();

        return joinEvents;
    }

    public async Task MakeParticipantNotExistAsync(int eventId, int participantId)
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

    public async Task<int> UpsertJoinEventAsync(JoinEvent updatedJoinEvent)
    {
        var isUpdate = updatedJoinEvent.Id != 0;

        if (isUpdate)
        {
            await UpdateJoinEventAsync(updatedJoinEvent);
            return updatedJoinEvent.Id;
        }

        //Attempt assuming all necessary nested entities are in the database
        try
        {
            var newId = await InsertJoinEventAsync(updatedJoinEvent);
            return newId;
        }
        catch (Exception)
        {
            //Add missing entities and try again.
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

    private async Task UpdateJoinEventAsync(JoinEvent updatedJoinEvent)
    {
        await context
            .JoinEvents.Where(b => b.Id == updatedJoinEvent.Id)
            .ExecuteUpdateAsync(setters =>
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
        var newParticipants = updatedJoinEvent.Participants!.Where(p => p.Id == 0).ToList();

        if (newParticipants.Count != 0)
        {
            foreach (var newParticipant in newParticipants)
            {
                foreach (var option in newParticipant.VotedFor)
                {
                    option.SelectedOptionId = option.SelectedOption.Id;
                    //don't track navigation property, because it is already tracked, which would make ef core try to insert it again
                    option.SelectedOption = null!;
                }

                //Make sure the new participant is linked to the updated join event
                newParticipant.JoinEventId = updatedJoinEvent.Id;
            }

            await context.Participants.AddRangeAsync(newParticipants);
        }

        await context.SaveChangesAsync();
    }

    private async Task<int> InsertJoinEventAsync(JoinEvent joinEvent)
    {
        context.ChangeTracker.Clear();

        //---The Order matters, certain entities need to be added before others---
        await HandleCinemaAndVersionTag(joinEvent);
        await HandlePlaytimeAndRoom(joinEvent);
        await HandleMoviesAndAgeRatings(joinEvent);
        await HandleHost(joinEvent);
        await HandleShowtimes(joinEvent);
        await HandleSelectOptions(joinEvent);
        var newJoinEventId = await HandleJoinEvent(joinEvent);
        await HandleParticipants(joinEvent);

        return newJoinEventId;
    }

    /// <summary>
    /// Set the cinemas and version tags to ef core references
    /// </summary>
    private async Task HandleCinemaAndVersionTag(JoinEvent joinEvent)
    {
        var cinemaIds = joinEvent.Showtimes.Select(st => st.Cinema.Id).Distinct().ToList();
        var versionTypes = joinEvent.Showtimes.Select(st => st.VersionTag.Type).Distinct().ToList();

        var existingCinemas = await context
            .Cinemas.Where(c => cinemaIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id);
        var existingVersionTags = await context
            .Versions.Where(v => versionTypes.Contains(v.Type))
            .ToDictionaryAsync(v => v.Type);

        foreach (var showtime in joinEvent.Showtimes)
        {
            if (!existingCinemas.TryGetValue(showtime.Cinema.Id, out var existingCinema))
            {
                throw new Exception($"Cinema {showtime.Cinema.Id} not in database");
            }

            showtime.Cinema = existingCinema;

            if (
                !existingVersionTags.TryGetValue(
                    showtime.VersionTag.Type,
                    out var existingVersionTag
                )
            )
            {
                throw new Exception($"Version {showtime.VersionTag.Type} not in database");
            }

            showtime.VersionTag = existingVersionTag;
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    ///  Upserts playtimes and rooms and sets them as ef core references
    /// </summary>
    private async Task HandlePlaytimeAndRoom(JoinEvent joinEvent)
    {
        var playtimeStartTimes = joinEvent.Showtimes.Select(st => st.Playtime.StartTime).Distinct();
        var roomIds = joinEvent.Showtimes.Select(st => st.Room.Id).Distinct().ToList();

        var existingPlaytimes = await context
            .Playtimes.Where(p => playtimeStartTimes.Contains(p.StartTime))
            .ToDictionaryAsync(p => p.StartTime);
        var existingRooms = await context
            .Rooms.Where(r => roomIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id);

        foreach (var showtime in joinEvent.Showtimes)
        {
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

    /// <summary>
    /// Upsert movies and age ratings and set them as ef core references
    /// </summary>
    private async Task HandleMoviesAndAgeRatings(JoinEvent joinEvent)
    {
        // Upsert all age ratings
        List<AgeRating> distinctAgeRatings = joinEvent
            .Showtimes.Where(st => st.Movie.AgeRating != null)
            .Select(st => st.Movie.AgeRating)
            .DistinctBy(a => a!.Censorship)
            .ToList()!;
        await context.AgeRatings.UpsertRange(distinctAgeRatings).On(a => a.Censorship).RunAsync();
        var existingAgeRatings = await context.AgeRatings.AsNoTracking().ToListAsync();

        // Collect all distinct movie IDs from the joinEvent's showtimes
        var movieIds = joinEvent.Showtimes.Select(st => st.Movie.Id).Distinct().ToList();

        // Fetch all existing movies in a single query
        var existingMovies = await context.Movies.Where(m => movieIds.Contains(m.Id)).ToListAsync();

        foreach (var movie in joinEvent.Showtimes.Select(st => st.Movie).DistinctBy(m => m.Id))
        {
            var existingMovie = existingMovies.FirstOrDefault(m => m.Id == movie.Id);

            if (existingMovie != null)
            {
                // Update the existing movie details
                existingMovie.KinoUrl = movie.KinoUrl;
                existingMovie.DurationInMinutes = movie.DurationInMinutes;
                existingMovie.ImageUrl = movie.ImageUrl;
                existingMovie.Title = movie.Title;
                existingMovie.PremiereDate = movie.PremiereDate;

                // Update age rating
                var existingAgeRating = existingAgeRatings.FirstOrDefault(ar =>
                    ar.Censorship == movie.AgeRating?.Censorship
                );
                if (existingAgeRating != null)
                {
                    movie.AgeRatingId = existingAgeRating.Id;
                    movie.AgeRating = null;
                }
            }
            else
            {
                // Update age rating
                var existingAgeRating = existingAgeRatings.FirstOrDefault(ar =>
                    ar.Censorship == movie.AgeRating?.Censorship
                );
                if (existingAgeRating != null)
                {
                    movie.AgeRatingId = existingAgeRating.Id;
                    movie.AgeRating = null;
                }

                // Add the new movie
                context.Movies.Add(movie);
            }
        }

        //make sure the age rating is not tracked, because it is already tracked,
        //which would make ef core try to insert it again.
        //The loop above uses DistinctBy, which means it might not handle all movies
        foreach (var movie in joinEvent.Showtimes.Select(st => st.Movie))
        {
            movie.AgeRating = null;
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Upsert the host and set it as ef core reference
    /// </summary>
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

    /// <summary>
    /// Upsert showtimes and set them as ef core references
    /// </summary>
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

    /// <summary>
    /// Upsert select options and set them as ef core references.
    /// Set the default select option to one of the upserted options.
    /// </summary>
    /// <remarks>Does not save to database, since it is saved afterwards in the HandleJoinEvent method,
    /// for optimization purposes by reducing amount of DB calls. </remarks>
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

        //Handle default select option
        var defaultSelectOption = joinEvent.DefaultSelectOption;

        // Get the existing default select option from the join event's list
        var existingDefaultSelectOption = joinEvent.SelectOptions.First(so =>
            so.VoteOption == defaultSelectOption.VoteOption && so.Color == defaultSelectOption.Color
        );

        // Use the existing option
        joinEvent.DefaultSelectOption = existingDefaultSelectOption;
        joinEvent.DefaultSelectOptionId = existingDefaultSelectOption.Id;
    }

    /// <summary>
    /// Insert the joinEvent and return the new id of the joinEvent
    ///</summary>
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
            Participants = [], //Participants are handled afterwards, but a joinEvent must exist beforehand
        };

        var newJointEventId = context.JoinEvents.Add(newJoinEvent);
        await context.SaveChangesAsync();
        joinEvent.Id = newJointEventId.Entity.Id;
        return newJointEventId.Entity.Id;
    }

    /// <summary>
    /// Insert participants
    /// </summary>
    private async Task HandleParticipants(JoinEvent joinEvent)
    {
        if (joinEvent.Participants.Any())
        {
            // Set selectOptions of participants to the ef core references
            var allSelectOptions = await context.SelectOptions.ToListAsync();
            foreach (
                var vote in joinEvent.Participants.SelectMany(participant => participant.VotedFor)
            )
            {
                var selectOption = allSelectOptions.FirstOrDefault(so =>
                    so.VoteOption == vote.SelectedOption.VoteOption
                    && so.Color == vote.SelectedOption.Color
                );

                if (selectOption != null)
                {
                    vote.SelectedOptionId = selectOption.Id; // Update the ID
                }
            }

            //Insert participants, this does not check if the participant already exists,
            //because this method is only called when a new joinEvent is inserted.
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
