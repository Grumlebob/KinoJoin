namespace Infrastructure.Services;

public class JoinEventService(KinoContext context) : IJoinEventService
{
    public async Task<int> UpsertJoinEventAsync(JoinEvent joinEvent)
    {
        var isUpdate = joinEvent.Id != 0;

        if (isUpdate)
        {
            var joinEventEntry = await context.JoinEvents.FindAsync(joinEvent.Id);
            if (joinEventEntry == null) return 0; // add

            joinEventEntry.Title = joinEvent.Title;
            joinEventEntry.Description = joinEvent.Description;
            joinEventEntry.ChosenShowtimeId = joinEvent.ChosenShowtimeId;
            joinEventEntry.Deadline = joinEvent.Deadline;

            await context.Participants.UpsertRange(joinEvent.Participants).RunAsync();
            joinEventEntry.Participants = context.Participants.Where(p => p.JoinEventId == joinEventEntry.Id).ToList();
            await context.SaveChangesAsync();
            return joinEventEntry.Id;
        }

        //upsert children
        await context.Hosts.Upsert(joinEvent.Host).RunAsync();
        await context
            .SelectOptions.UpsertRange(joinEvent.SelectOptions).On(s => new { s.VoteOption, s.Color }).RunAsync();

        var playTimesToUpsert = joinEvent.Showtimes.Select(st => st.Playtime).DistinctBy(p => p.StartTime);
        await context.Playtimes.UpsertRange(playTimesToUpsert).On(p => p.StartTime).RunAsync();

        var roomsToUpsert = joinEvent.Showtimes.Select(st => st.Room).DistinctBy(r => r.Id).ToList();
        await context.Rooms.UpsertRange(roomsToUpsert).RunAsync();
        

        foreach (var showtime in joinEvent.Showtimes)
        {
            //Set navigation properties
            showtime.MovieId = showtime.Movie.Id;
            showtime.CinemaId = showtime.Cinema.Id;
            showtime.RoomId = showtime.Room.Id;
            showtime.PlaytimeId = (await context.Playtimes.AsNoTracking().FirstAsync(p => p.StartTime == showtime.Playtime.StartTime))
                .Id; //id's are genrated in database. Get them from there
        }

        try
        {
            return await TryInsertJoinEvent(joinEvent);
        }
        catch (Exception)
        {
            await UpsertMissingEntities(joinEvent);
            return await TryInsertJoinEvent(joinEvent);
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
    }

    private async Task<int> TryInsertJoinEvent(JoinEvent joinEvent)
    {
        //await AssignPlaytimeAndVersionIdFromDatabase(joinEvent.Showtimes);
        foreach (var showtime in joinEvent.Showtimes)
        {
            //this asssumes we have versiontags, if it fails we insert missing entities
            showtime.VersionTagId = (await context.Versions.AsNoTracking().FirstAsync(v => v.Type == showtime.VersionTag.Type)).Id;
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

        joinEvent.JoinEventSelectOptions = joinEvent.SelectOptions
            .Select(s => new JoinEventSelectOption { SelectOptionsId = s.Id }).ToList();

        // Debugging:
        context.ChangeTracker.DetectChanges();
        Console.WriteLine(context.ChangeTracker.DebugView.LongView);

        var newlyAddedJoinEvent = await context.JoinEvents.AddAsync(joinEvent);
        
        await context.SaveChangesAsync();
        
        return newlyAddedJoinEvent.Entity.Id;
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