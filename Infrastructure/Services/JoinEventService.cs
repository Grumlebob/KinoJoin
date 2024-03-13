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

        await UpsertMissingEntities(updatedJoinEvent);

        if (isUpdate)
        {
            await context.JoinEvents.ExecuteUpdateAsync(
                setters => setters.SetProperty(b => b.Title, updatedJoinEvent.Title)
                    .SetProperty(b => b.Description, updatedJoinEvent.Description)
                    .SetProperty(b => b.ChosenShowtimeId, updatedJoinEvent.ChosenShowtimeId)
                    .SetProperty(b => b.Deadline, updatedJoinEvent.Deadline));

            context.ChangeTracker.Clear();
            // Fetch existing participants from the database
            var existingParticipants = await context.Participants
                .Where(p => p.JoinEventId == updatedJoinEvent.Id)
                .ToListAsync();

            // Update existing participants
            foreach (var existingParticipant in existingParticipants)
            {
                var updatedParticipant = updatedJoinEvent.Participants
                    .FirstOrDefault(p => p.Id == existingParticipant.Id);

                if (updatedParticipant != null)
                {
                    context.Entry(existingParticipant).CurrentValues.SetValues(updatedParticipant);
                }
            }

            // Identify and add new participants
            var newParticipants = updatedJoinEvent.Participants
                .Where(p => p.Id == 0)
                .ToList();

            if (newParticipants.Any())
            {
                foreach (var newParticipant in newParticipants)
                {
                    newParticipant.JoinEventId = updatedJoinEvent.Id;
                    await context.Participants.AddAsync(newParticipant);
                }

                await context.Participants.AddRangeAsync(newParticipants);
            }

            await context.SaveChangesAsync();

            return updatedJoinEvent.Id;
        }

        //INSERTING
        var newId = await JacobInsert(updatedJoinEvent);
        return newId;
    }

    private async Task<int> JacobInsert(JoinEvent joinEvent)
    {
        context.ChangeTracker.Clear();

        // Process SelectOptions
        foreach (var selectOption in joinEvent.SelectOptions)
        {
            await ProcessSelectOption(selectOption);
        }

        // Process DefaultSelectOption if not null
        if (joinEvent.DefaultSelectOption != null)
        {
            await ProcessSelectOption(joinEvent.DefaultSelectOption);
        }

        await context.SaveChangesAsync();

        foreach (var st in joinEvent.Showtimes)
        {
            var cinemaIds = joinEvent.Showtimes.Select(st => st.Cinema.Id).Distinct();
            foreach (var cinemaId in cinemaIds)
            {
                var existingCinema = await context.Cinemas.FindAsync(cinemaId);
                if (existingCinema == null)
                {
                    // This should only happen if you're sure you want to add new Cinemas
                    var cinemaName = joinEvent.Showtimes.FirstOrDefault(st => st.Cinema.Id == cinemaId)?.Cinema
                        .Name;
                    context.Cinemas.Add(new Cinema { Id = cinemaId, Name = cinemaName });
                }
                else
                {
                    // Attach existing cinemas to each showtime
                    foreach (var showtime in joinEvent.Showtimes.Where(st => st.Cinema.Id == cinemaId))
                    {
                        showtime.Cinema = existingCinema;
                    }
                }
            }

            // Handle Playtime
            var existingPlaytime =
                await context.Playtimes.FirstOrDefaultAsync(p => p.StartTime == st.Playtime.StartTime);
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
            var existingVersionTag = await context.Versions.FirstOrDefaultAsync(v => v.Type == st.VersionTag.Type);
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

        //Handle movies Same way as cinemas
        foreach (var st in joinEvent.Showtimes)
        {
            var movieIds = joinEvent.Showtimes.Select(st => st.Movie.Id).Distinct();
            foreach (var movieId in movieIds)
            {
                var existingMovie = await context.Movies.FindAsync(movieId);
                if (existingMovie == null)
                {
                    //find the correct movie in st
                    var movie = joinEvent.Showtimes.FirstOrDefault(st => st.Movie.Id == movieId)?.Movie;

                    // This should only happen if you're sure you want to add new Movies
                    context.Movies.Add(new Movie
                    {
                        Id = movieId, //insert all properties
                        KinoURL = movie.KinoURL,
                        AgeRating = movie.AgeRating,
                        Duration = movie.Duration,
                        ImageUrl = movie.ImageUrl,
                        Title = movie.Title,
                        PremiereDate = movie.PremiereDate
                    }); // Specify other properties
                }
                else
                {
                    // Attach existing movies to each showtime
                    foreach (var showtime in joinEvent.Showtimes.Where(st => st.Movie.Id == movieId))
                    {
                        showtime.Movie = existingMovie;
                    }
                }
            }
        }

        await context.SaveChangesAsync();

        // Handle Host
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

        //-------Handle showtimes-------------
        foreach (var showtime in joinEvent.Showtimes)
        {
            var existingShowtime = await context.Showtimes.FindAsync(showtime.Id);
            if (existingShowtime != null)
            {
                context.Showtimes.Attach(existingShowtime);
                showtime.Id = existingShowtime.Id;
            }
            else
            {
                //add new showtime, with only the Ids of the related entities
                var newShowtime = new Showtime
                {
                    Id = showtime.Id,
                    MovieId = showtime.Movie.Id,
                    CinemaId = showtime.Cinema.Id,
                    PlaytimeId = showtime.Playtime.Id,
                    VersionTagId = showtime.VersionTag.Id,
                    RoomId = showtime.Room.Id
                };

                context.Showtimes.Add(newShowtime);
            }
        }

        await context.SaveChangesAsync();

        var ShowtimesToAttach = new List<Showtime>();
        foreach (var showtime in joinEvent.Showtimes)
        {
            var existingShowtime = await context.Showtimes.FindAsync(showtime.Id);
            if (existingShowtime != null)
            {
                ShowtimesToAttach.Add(existingShowtime);
            }
            else
            {
                ShowtimesToAttach.Add(showtime);
            }
        }
        //-------------------------------

        //Handle SelectOptions, add keys to joinEvent
        foreach (var selectOption in joinEvent.SelectOptions)
        {
            //find existing based on color and voteOption
            var existingSelectOption = await context.SelectOptions
                .FirstOrDefaultAsync(so => so.VoteOption == selectOption.VoteOption && so.Color == selectOption.Color);
            if (existingSelectOption != null)
            {
                context.SelectOptions.Attach(existingSelectOption);
                selectOption.Id = existingSelectOption.Id;
            }
            else
            {
                context.SelectOptions.Add(selectOption);
            }
        }

        var SelectOptionsToAttach = new List<SelectOption>();
        foreach (var selectOption in joinEvent.SelectOptions)
        {
            var existingSelectOption = await context.SelectOptions
                .FirstOrDefaultAsync(so => so.VoteOption == selectOption.VoteOption && so.Color == selectOption.Color);

            if (existingSelectOption != null)
            {
                SelectOptionsToAttach.Add(existingSelectOption);
            }
            else
            {
                SelectOptionsToAttach.Add(selectOption);
            }
        }

        //Handle DefaultSelectOption
        if (joinEvent.DefaultSelectOption != null)
        {
            var existingDefaultSelectOption = await context.SelectOptions
                .FirstOrDefaultAsync(so =>
                    so.VoteOption == joinEvent.DefaultSelectOption.VoteOption &&
                    so.Color == joinEvent.DefaultSelectOption.Color);
            if (existingDefaultSelectOption != null)
            {
                joinEvent.DefaultSelectOptionId = existingDefaultSelectOption.Id;
                joinEvent.DefaultSelectOption = existingDefaultSelectOption;
            }
            else
            {
                context.SelectOptions.Add(joinEvent.DefaultSelectOption);
            }
        }

        var defaultSelectOptionToAttach = new SelectOption();
        if (joinEvent.DefaultSelectOption != null)
        {
            var existingDefaultSelectOption = await context.SelectOptions
                .FirstOrDefaultAsync(so =>
                    so.VoteOption == joinEvent.DefaultSelectOption.VoteOption &&
                    so.Color == joinEvent.DefaultSelectOption.Color);
            if (existingDefaultSelectOption != null)
            {
                defaultSelectOptionToAttach = existingDefaultSelectOption;
            }
            else
            {
                defaultSelectOptionToAttach = joinEvent.DefaultSelectOption;
            }
        }

        // Handle JoinEvent
        var newJoinEventId = 0;
        joinEvent.HostId = joinEvent.Host.AuthId;

        var newJoinEvent = new JoinEvent
        {
            Showtimes = ShowtimesToAttach,
            Id = joinEvent.Id,
            Title = joinEvent.Title,
            Description = joinEvent.Description,
            Deadline = joinEvent.Deadline,
            HostId = joinEvent.Host.AuthId,
            ChosenShowtimeId = joinEvent.ChosenShowtimeId,
            DefaultSelectOptionId = defaultSelectOptionToAttach.Id,
            SelectOptions = SelectOptionsToAttach,
            Participants = null,
        };

        var addedId = context.JoinEvents.Add(newJoinEvent);
        await context.SaveChangesAsync();

        //Handle VotedFor
        //Update ParticipantVote records with the correct SelectedOptionId
        foreach (var participant in joinEvent.Participants)
        {
            foreach (var vote in participant.VotedFor)
            {
                var selectOption = await context.SelectOptions
                    .FirstOrDefaultAsync(so =>
                        so.VoteOption == vote.SelectedOption.VoteOption && so.Color == vote.SelectedOption.Color);

                if (selectOption != null)
                {
                    vote.SelectedOptionId = selectOption.Id; // Update the ID
                }
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
                VotedFor = p.VotedFor.Select(v =>
                    new ParticipantVote()
                    {
                        ParticipantId = p.Id, ShowtimeId = ShowtimesToAttach.First(s => s.Id == v.ShowtimeId).Id,
                        SelectedOptionId = v.SelectedOptionId
                    }).ToList()
            };

            await context.Participants.AddAsync(participant);
        }

        await context.SaveChangesAsync();

        return addedId.Entity.Id;
    }

    private async Task ProcessSelectOption(SelectOption option)
    {
        var existingOption = await context.SelectOptions
            .AsNoTracking()
            .FirstOrDefaultAsync(so => so.VoteOption == option.VoteOption && so.Color == option.Color);

        if (existingOption != null)
        {
            // Existing option found, update the ID of the current option
            option.Id = existingOption.Id;
        }
        else
        {
            // Check if the context is already tracking this SelectOption
            var trackedOption = context.ChangeTracker.Entries<SelectOption>()
                .FirstOrDefault(e => e.Entity.VoteOption == option.VoteOption && e.Entity.Color == option.Color)
                ?.Entity;

            if (trackedOption == null)
            {
                // Option is new and not being tracked, add it
                await context.SelectOptions.AddAsync(option);
            }
            // If it's already being tracked, do nothing
        }
    }


    private async Task UpsertMissingEntities(JoinEvent joinEvent)
    {
        var movies = joinEvent.Showtimes.Select(st => st.Movie).DistinctBy(m => m.Id);
        await context.Movies.UpsertRange(movies).RunAsync();

        var cinemas = joinEvent.Showtimes.Select(st => st.Cinema).DistinctBy(c => c.Id);
        await context.Cinemas.UpsertRange(cinemas).RunAsync();

        var versions = joinEvent
            .Showtimes.Select(st => st.VersionTag)
            .DistinctBy(v => v.Type);
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
            .Include(j => j.Participants)
            .ThenInclude(p => p.VotedFor).ThenInclude(pv => pv.SelectedOption)
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
            .Include(j => j.Participants)
            .ThenInclude(p => p.VotedFor).ThenInclude(pv => pv.SelectedOption)
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