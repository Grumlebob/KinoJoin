namespace Infrastructure.Services;

public class JoinEventService(KinoContext context) : IJoinEventService
{
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
            await UpsertMissingEntities(updatedJoinEvent);
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
                        if (option.SelectedOption != null)
                        {
                            option.SelectedOptionId = option.SelectedOption.Id;
                        }
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

        //Handle Cinemas, Playtimes, VersionTags, Sal, Movies
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
                Console.WriteLine("Existing version tag: " + existingVersionTag.Type);
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

        //Handle movies
        foreach (var _ in joinEvent.Showtimes)
        {
            var movieIds = joinEvent.Showtimes.Select(st => st.Movie.Id).Distinct();
            foreach (var movieId in movieIds)
            {
                var existingMovie = await context.Movies.FindAsync(movieId);
                if (existingMovie == null)
                {
                    var movie = joinEvent
                        .Showtimes.FirstOrDefault(st => st.Movie.Id == movieId)
                        ?.Movie;

                    if (movie != null)
                        context.Movies.Add(
                            new Movie
                            {
                                Id = movieId,
                                KinoURL = movie.KinoURL,
                                AgeRating = movie.AgeRating,
                                Duration = movie.Duration,
                                ImageUrl = movie.ImageUrl,
                                Title = movie.Title,
                                PremiereDate = movie.PremiereDate
                            }
                        );
                }
                else
                {
                    // Attach existing movies to each showtime
                    foreach (
                        var showtime in joinEvent.Showtimes.Where(st => st.Movie.Id == movieId)
                    )
                    {
                        showtime.Movie = existingMovie;
                    }
                }
            }
        }

        await context.SaveChangesAsync();

        //Handle Host
        var existingHost = await context.Hosts.FindAsync(joinEvent.Host.AuthId);
        if (existingHost != null)
        {
            context.Hosts.Attach(existingHost);
            joinEvent.Host = existingHost;
        }
        else
        {
            context.Hosts.Add(joinEvent.Host);
        }

        await context.SaveChangesAsync();

        //Handle showtimes
        var processedShowtimes = await HandleShowtimes(joinEvent.Showtimes);

        //Handle SelectOptions
        var selectOptionsToAttach = new List<SelectOption>();
        if (joinEvent.SelectOptions != null)
        {
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
                }
                else
                {
                    // No existing option found, add a new one
                    context.SelectOptions.Add(selectOption);
                    selectOptionsToAttach.Add(selectOption); // Add the new one
                }
            }

            await context.SaveChangesAsync();

            // Handle DefaultSelectOption

            var existingDefaultSelectOption = await context.SelectOptions.FirstOrDefaultAsync(so =>
                so.VoteOption == joinEvent.DefaultSelectOption.VoteOption
                && so.Color == joinEvent.DefaultSelectOption.Color
            );

            // Existing option found, use it for the JoinEvent
            if (existingDefaultSelectOption != null)
            {
                joinEvent.DefaultSelectOption = existingDefaultSelectOption;
                joinEvent.DefaultSelectOptionId = existingDefaultSelectOption.Id;
            }
            else
            {
                // No existing option found, add new DefaultSelectOption to the database
                context.SelectOptions.Add(joinEvent.DefaultSelectOption);
                await context.SaveChangesAsync(); // Ensure the DefaultSelectOption gets an ID
                joinEvent.DefaultSelectOptionId = joinEvent.DefaultSelectOption.Id;
            }

            // Handle JoinEvent
            joinEvent.HostId = joinEvent.Host.AuthId;

            var newJoinEvent = new JoinEvent
            {
                Showtimes = processedShowtimes,
                Id = joinEvent.Id,
                Title = joinEvent.Title,
                Description = joinEvent.Description,
                Deadline = joinEvent.Deadline,
                HostId = joinEvent.Host.AuthId,
                ChosenShowtimeId = joinEvent.ChosenShowtimeId,
                DefaultSelectOptionId = joinEvent.DefaultSelectOptionId,
                SelectOptions = selectOptionsToAttach,
                Participants = null,
            };

            var addedId = context.JoinEvents.Add(newJoinEvent);
            await context.SaveChangesAsync();

            //We need to handle participants, after JoinEvent is added, so we can get the ID
            //Handle VotedFor - Update ParticipantVote records with the correct SelectedOptionId
            if (joinEvent.Participants != null)
            {
                foreach (
                    var vote in joinEvent.Participants.SelectMany(participant =>
                        participant.VotedFor
                    )
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
                        JoinEventId = addedId.Entity.Id,
                        Nickname = p.Nickname,
                        Email = p.Email,
                        Note = p.Note,
                        VotedFor = p
                            .VotedFor.Select(v => new ParticipantVote()
                            {
                                ParticipantId = p.Id,
                                ShowtimeId = processedShowtimes.First(s => s.Id == v.ShowtimeId).Id,
                                SelectedOptionId = v.SelectedOptionId
                            })
                            .ToList()
                    };

                    await context.Participants.AddAsync(participant);
                }
            }

            await context.SaveChangesAsync();

            return addedId.Entity.Id;
        }
        else
        {
            throw new Exception("No SelectOptions found");
        }
    }

    private async Task<List<Showtime>> HandleShowtimes(IEnumerable<Showtime> showtimes)
    {
        var showtimesToAttach = new List<Showtime>();

        foreach (var showtime in showtimes)
        {
            var existingShowtime =
                await context.Showtimes.FindAsync(showtime.Id)
                ?? context
                    .Showtimes.Add(
                        new Showtime
                        {
                            Id = showtime.Id,
                            MovieId = showtime.Movie.Id,
                            CinemaId = showtime.Cinema.Id,
                            PlaytimeId = showtime.Playtime.Id,
                            VersionTagId = showtime.VersionTag.Id,
                            RoomId = showtime.Room.Id
                        }
                    )
                    .Entity;
            showtimesToAttach.Add(existingShowtime);
        }

        await context.SaveChangesAsync();
        return showtimesToAttach;
    }

    private async Task UpsertMissingEntities(JoinEvent joinEvent)
    {
        var movies = joinEvent.Showtimes.Select(st => st.Movie).DistinctBy(m => m.Id);
        await context.Movies.UpsertRange(movies).RunAsync();

        var cinemas = joinEvent.Showtimes.Select(st => st.Cinema).DistinctBy(c => c.Id);
        await context.Cinemas.UpsertRange(cinemas).RunAsync();

        var versions = joinEvent.Showtimes.Select(st => st.VersionTag).DistinctBy(v => v.Type);
        await context.Versions.UpsertRange(versions).On(v => v.Type).RunAsync();
    }

    public async Task<JoinEvent?> GetAsync(int id)
    {
        var result = await context
            .JoinEvents.AsNoTracking()
            .Include(j => j.Showtimes)
            .ThenInclude(s => s.Movie)
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

    public async Task<List<JoinEvent>> GetAllAsync(Func<JoinEvent, bool>? filter = null)
    {
        var query = context.JoinEvents.AsNoTracking();
        if (filter != null)
        {
            query = query.AsEnumerable().Where(filter).AsQueryable();
        }

        //Ensure all nested entities are included
        var joinEvents = await query
            .Include(j => j.Showtimes)
            .ThenInclude(s => s.Movie)
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

        if (filter != null)
        {
            joinEvents = joinEvents.Where(filter).ToList();
        }

        return joinEvents;
    }
}
