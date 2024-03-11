namespace Infrastructure.Services;

public class JoinEventService(KinoContext context) : IJoinEventService
{
    public async Task<int> UpsertJoinEventAsync(UpsertJoinEventDto joinEventDto)
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

        var joinEvent = UpsertJoinEventDto.FromUpsertDtoToModel(joinEventDto);
        
        
        var existingJoinEvent = await context.JoinEvents.FindAsync(joinEvent.Id);
        if (existingJoinEvent != null)
        {
            joinEvent = existingJoinEvent;
            context.JoinEvents.Attach(joinEvent);
        }

        //Stage 1
        await context.Hosts.Upsert(joinEvent.Host).RunAsync();

        // Avoid creating a new host, when upserting join event, since it would try to insert the host again
        joinEvent.Host = null!;

        await context
            .SelectOptions.UpsertRange(joinEvent.SelectOptions)
            .On(s => new { s.VoteOption, s.Color })
            .RunAsync();

        var playTimes = joinEvent.Showtimes.Select(st => st.Playtime).DistinctBy(p => p.StartTime);
        await context.Playtimes.UpsertRange(playTimes).On(p => p.StartTime).RunAsync();

        var newRooms = joinEvent.Showtimes.Select(st => st.Room).DistinctBy(r => r.Id).ToList();
        await context.Rooms.UpsertRange(newRooms).RunAsync();
        await context.SaveChangesAsync();

        //Stage 2
        try
        {
            //2.1
            return await TryUpsertJoinEvent(joinEventDto, joinEvent);
        }
        catch (Exception)
        {
            //2.2
            await UpsertMissingEntities(joinEvent);

            return await TryUpsertJoinEvent(joinEventDto, joinEvent);
        }
    }

    private async Task<int> TryUpsertJoinEvent(UpsertJoinEventDto joinEventDto, JoinEvent joinEvent)
    {
        await AssignPlaytimeAndVersionIdFromDatabase(joinEvent.Showtimes);

        await context.Showtimes.UpsertRange(joinEvent.Showtimes).RunAsync();
        await context.SaveChangesAsync();

        bool isUpdate = joinEventDto.Id != null && joinEventDto.Id != 0;

        if (isUpdate)
        {
            //Ensure that entities in many to many relationships that are already in the database are not added again,
            //when the join event is updated, to avoid duplication issues

            // Participant ids are 0 if they are yet to be added to database
            joinEvent.Participants.RemoveAll(p => p.Id != 0);

            joinEvent.SelectOptions.RemoveAll(s =>
                context.SelectOptions.Any(dto =>
                    dto.VoteOption == s.VoteOption && dto.Color == s.Color
                )
            );

            // The list of showtimes cannot change after the joinEvent is created
            joinEvent.Showtimes = [];
        }
        else
        {
            //Use the entities from the database for many to many relationships,
            //so EF Core doesn't try to add them again, when the join event is added.
            
            var showtimeIds = joinEventDto.Showtimes.Select(s => s.Id).ToList();
            joinEvent.Showtimes = context.Showtimes.Where(s => showtimeIds.Contains(s.Id)).ToList();
            joinEvent.SelectOptions = context
                .SelectOptions.Where(s =>
                    context.SelectOptions.Any(o => o.VoteOption == s.VoteOption && o.Color == s.Color)
                )
                .ToList();
           
        }
        


        var newlyUpsertedJoinEvent = isUpdate
            ? context.JoinEvents.Update(joinEvent)
            : await context.JoinEvents.AddAsync(joinEvent);

        await context.SaveChangesAsync();
        return newlyUpsertedJoinEvent.Entity.Id;
    }

    private async Task UpsertMissingEntities(JoinEvent joinEventWithNavProps)
    {
        var movies = joinEventWithNavProps.Showtimes.Select(st => st.Movie).DistinctBy(m => m.Id);
        await context.Movies.UpsertRange(movies).RunAsync();

        var cinemas = joinEventWithNavProps.Showtimes.Select(st => st.Cinema).DistinctBy(c => c.Id);
        await context.Cinemas.UpsertRange(cinemas).RunAsync();

        var versions = joinEventWithNavProps
            .Showtimes.Select(st => st.VersionTag)
            .DistinctBy(v => v.Type);
        await context.Versions.UpsertRange(versions).On(v => v.Type).RunAsync();

        await AssignPlaytimeAndVersionIdFromDatabase(joinEventWithNavProps.Showtimes);

        await context.Showtimes.UpsertRange(joinEventWithNavProps.Showtimes).RunAsync();
        await context.SaveChangesAsync();
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

        //avoid circular reference
        result?.Showtimes.ForEach(s => s.JoinEvents = []);
        result?.SelectOptions.ForEach(s => s.JoinEvents = []);
        result?.Host.JoinEvents.Clear();
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

        //avoid circular reference
        foreach (var joinEvent in joinEvents)
        {
            joinEvent.Showtimes.ForEach(s => s.JoinEvents = []);
            joinEvent.SelectOptions.ForEach(s => s.JoinEvents = []);
            joinEvent.Host.JoinEvents.Clear();
        }

        return joinEvents;
    }
}
