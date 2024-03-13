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
            await context.JoinEvents.ExecuteUpdateAsync(
                setters => setters.SetProperty(b => b.Title, updatedJoinEvent.Title)
                    .SetProperty(b => b.Description, updatedJoinEvent.Description)
                    .SetProperty(b => b.ChosenShowtimeId, updatedJoinEvent.ChosenShowtimeId)
                    .SetProperty(b => b.Deadline, updatedJoinEvent.Deadline));

            await UpdateJoinEventParticipantsAsync(updatedJoinEvent);

            return updatedJoinEvent.Id;
        }

        //upsert children
        await context.Hosts.Upsert(updatedJoinEvent.Host).RunAsync();
        await context
            .SelectOptions.UpsertRange(updatedJoinEvent.SelectOptions).On(s => new { s.VoteOption, s.Color })
            .RunAsync();

        var playTimesToUpsert = updatedJoinEvent.Showtimes.Select(st => st.Playtime).DistinctBy(p => p.StartTime);
        await context.Playtimes.UpsertRange(playTimesToUpsert).On(p => p.StartTime).RunAsync();

        var roomsToUpsert = updatedJoinEvent.Showtimes.Select(st => st.Room).DistinctBy(r => r.Id).ToList();
        await context.Rooms.UpsertRange(roomsToUpsert).RunAsync();

        //Set navigation properties
        updatedJoinEvent.DefaultSelectOptionId = (await context.SelectOptions.AsNoTracking()
            .FirstAsync(s =>
                s.VoteOption == updatedJoinEvent.DefaultSelectOption.VoteOption &&
                s.Color == updatedJoinEvent.DefaultSelectOption.Color)).Id;
        updatedJoinEvent.DefaultSelectOption = null!; //avoid double tracking when adding


        foreach (var vote in updatedJoinEvent.Participants.SelectMany(participant => participant.VotedFor))
        {
            vote.SelectedOptionId = (await context.SelectOptions.AsNoTracking()
                .FirstAsync(s =>
                    s.VoteOption == vote.SelectedOption.VoteOption && s.Color == vote.SelectedOption.Color)).Id;
            vote.SelectedOption = null!; //avoid double tracking when adding
        }

        foreach (var showtime in updatedJoinEvent.Showtimes)
        {
            showtime.MovieId = showtime.Movie.Id;
            showtime.CinemaId = showtime.Cinema.Id;
            showtime.RoomId = showtime.Room.Id;
            showtime.PlaytimeId = (await context.Playtimes.AsNoTracking()
                    .FirstAsync(p => p.StartTime == showtime.Playtime.StartTime))
                .Id; //id's are genrated in database. Get them from there
        }

        try
        {
            return await TryInsertJoinEvent(updatedJoinEvent);
        }
        catch (Exception)
        {
            await UpsertMissingEntities(updatedJoinEvent);
            return await TryInsertJoinEvent(updatedJoinEvent);
        }
    }

    private async Task UpdateJoinEventParticipantsAsync(JoinEvent updatedJoinEvent)
    {
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
    }


    private async Task<int> TryInsertJoinEvent(JoinEvent joinEvent)
    {
        //await AssignPlaytimeAndVersionIdFromDatabase(joinEvent.Showtimes);
        foreach (var showtime in joinEvent.Showtimes)
        {
            //this asssumes we have versiontags, if it fails we insert missing entities and try again
            showtime.VersionTagId =
                (await context.Versions.AsNoTracking().FirstAsync(v => v.Type == showtime.VersionTag.Type)).Id;
        }

        await context.Showtimes.UpsertRange(joinEvent.Showtimes).RunAsync();

        //Use the entities from the database for many to many relationships,
        //so EF Core doesn't try to add them again, when the join event is added.

        //var showtimeIds = joinEvent.Showtimes.Select(s => s.Id).ToList();
        //joinEvent.Showtimes = context.Showtimes.Where(s => showtimeIds.Contains(s.Id)).ToList();
        joinEvent.Showtimes = joinEvent.Showtimes.Select(s => new Showtime { Id = s.Id }).ToList();
        foreach (var option in joinEvent.SelectOptions)
        {
            option.Id = (await context.SelectOptions.AsNoTracking().FirstAsync(s =>
                s.VoteOption == option.VoteOption && s.Color == option.Color)).Id;
        }

        context.ChangeTracker.DetectChanges();
        Console.WriteLine(context.ChangeTracker.DebugView.LongView);

        var newlyAddedJoinEvent = (context.JoinEvents.Add(joinEvent)).Entity;
        context.ChangeTracker.DetectChanges();
        Console.WriteLine(context.ChangeTracker.DebugView.LongView);

        foreach (var option in newlyAddedJoinEvent.SelectOptions)
        {
            context.Entry(option).State = EntityState.Unchanged;
        }

        context.Entry(newlyAddedJoinEvent.Host).State = EntityState.Unchanged;

        foreach (var option in newlyAddedJoinEvent.Showtimes)
        {
            context.Entry(option).State = EntityState.Unchanged;
        }

        context.ChangeTracker.DetectChanges();
        Console.WriteLine(context.ChangeTracker.DebugView.LongView);
        await context.SaveChangesAsync();

        return newlyAddedJoinEvent.Id;
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

    private async Task AssignPlaytimeAndVersionIdFromDatabase(List<Showtime> showtimes)
    {
        // Retrieve all distinct StartTimes and Types from the showtimes.
        var distinctPlaytimes = showtimes.Select(st => st.Playtime.StartTime).Distinct();
        var distinctVersionTypes = showtimes.Select(st => st.VersionTag.Type).Distinct();

        // Retrieve all matching Playtimes and VersionTags from the database.
        var playtimes = await context
            .Playtimes.Where(p => distinctPlaytimes.Contains(p.StartTime))
            .ToListAsync();
        var versions = await context
            .Versions.Where(v => distinctVersionTypes.Contains(v.Type))
            .ToListAsync();

        var versionDict = versions.ToDictionary(v => v.Type, v => v.Id);
        var playtimeDict = playtimes.ToDictionary(p => p.StartTime.ToOADate(), p => p.Id);

        foreach (var showtime in showtimes)
        {
            var lookupKey = showtime.Playtime.StartTime.ToOADate();

            // Assign PlaytimeId and VersionTagId using the dictionaries.
            if (versionDict.TryGetValue(showtime.VersionTag.Type, out var versionTagId))
            {
                showtime.VersionTagId = versionTagId;
            }

            if (playtimeDict.TryGetValue(lookupKey, out var playtimeId))
            {
                showtime.PlaytimeId = playtimeId;
            }
        }
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
            .ThenInclude(p => p.VotedFor)
            .Include(j => j.SelectOptions)
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
            .ThenInclude(p => p.VotedFor)
            .Include(j => j.SelectOptions)
            .Include(j => j.Host)
            .ToListAsync();

        if (filter != null)
        {
            joinEvents = joinEvents.Where(filter).ToList();
        }

        return joinEvents;
    }
}