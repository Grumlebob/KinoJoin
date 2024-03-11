using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Infrastructure.Services;

/**
 * The overall goal is managing the database operations for JoinEvent
 **/

public class JoinEventService(KinoContext context) : IJoinEventService
{
    /*
     * The overall goal is to save the entire workpage of a JoinEvent, regardless of creating or filling
     * 
     * Stages:
     * 1. We need to upsert entities that needs to exist in the database before JoinEvent is added.
     * 2. Upon program startup, the DB is seeded with a lot movies, cinemas, playtimes, versions and rooms.
     * 2.1 We try to insert the joinEvent using the preseeded data in the database.
     * 2.2 In unlikely scenario it fails, Kino have updated the database with new movies, cinemas, playtimes, versions and rooms, which will be added.
     * 3. If the JoinEventDto has no ID, we will insert the JoinEvent with the preseeded data, otherwise it will be updated
     */
    public async Task<int> UpsertJoinEventAsync(UpsertJoinEventDto joinEventDto)
    {
        var joinEvent = UpsertJoinEventDto.FromUpsertDtoToModel(joinEventDto);

        await context.Hosts.Upsert(joinEvent.Host).RunAsync();

        await context
            .SelectOptions.UpsertRange(joinEvent.SelectOptions)
            .On(s => new { s.VoteOption, s.Color })
            .RunAsync();

        var playTimes = joinEvent
            .Showtimes.Select(st => st.Playtime)
            .DistinctBy(p => p.StartTime);
        await context.Playtimes.UpsertRange(playTimes).On(p => p.StartTime).RunAsync();

        var newRooms = joinEvent
            .Showtimes.Select(st => st.Room)
            .DistinctBy(r => r.Id)
            .ToList();
        await context.Rooms.UpsertRange(newRooms).RunAsync();
        await context.SaveChangesAsync();

        try
        {
            return await HandleJoinEventUpsert(joinEventDto, joinEvent);
        }
        catch (Exception _)
        {
            await UpsertMissingEntities(joinEvent);
            return await HandleJoinEventUpsert(joinEventDto, joinEvent);
        }
    }

    private async Task<int> HandleJoinEventUpsert(
        UpsertJoinEventDto joinEventDto,
        JoinEvent joinEvent
    )
    {
        await AttachPlaytimeAndVersionId(joinEvent.Showtimes);

        await context.Showtimes.UpsertRange(joinEvent.Showtimes).RunAsync();
        await context.SaveChangesAsync();

        joinEvent.Host = null!; // Avoid creating a new host

        bool isUpdate = joinEventDto.Id != null;
        PrepareJoinEventManyToManyRelationships(joinEventDto, joinEvent, isUpdate);

        var newlyUpsertedJoinEvent = isUpdate
            ? context.JoinEvents.Update(joinEvent)
            : await context.JoinEvents.AddAsync(joinEvent);

        await context.SaveChangesAsync();
        return newlyUpsertedJoinEvent.Entity.Id;
    }

    private void PrepareJoinEventManyToManyRelationships(
        UpsertJoinEventDto joinEventDto,
        JoinEvent joinEvent,
        bool isUpdate
    )
    {
        var voteOptions = joinEventDto.SelectOptions.Select(s => s.VoteOption);
        var colorOptions = joinEventDto.SelectOptions.Select(s => s.Color);

        if (isUpdate)
        {
            // They are 0 if yet to be added to database
            joinEvent.Participants.RemoveAll(p => p.Id != 0);
            //Don't add SelectOptions that already exist
            joinEvent.SelectOptions.RemoveAll(s =>
                voteOptions.Contains(s.VoteOption) && colorOptions.Contains(s.Color)
            );
            //Showtimes cannot change after joinEvent is created
            joinEvent.Showtimes = [];
        }
        else
        {
            //We need to ensure many to many relationships, are only added once
            var showtimeIds = joinEventDto.Showtimes.Select(s => s.Id);
            joinEvent.Showtimes = context
                .Showtimes.Where(s => showtimeIds.Contains(s.Id))
                .ToList();
            joinEvent.SelectOptions = context
                .SelectOptions.Where(s =>
                    voteOptions.Contains(s.VoteOption) && colorOptions.Contains(s.Color)
                )
                .ToList();
        }
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

        await AttachPlaytimeAndVersionId(joinEventWithNavProps.Showtimes);

        await context.Showtimes.UpsertRange(joinEventWithNavProps.Showtimes).RunAsync();
        await context.SaveChangesAsync();
    }

    private async Task AttachPlaytimeAndVersionId(List<Showtime> showtimes)
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
            var LookupKey = showtime.Playtime.StartTime.ToOADate();

            // Assign PlaytimeId and VersionTagId using the dictionaries.
            if (versionDict.TryGetValue(showtime.VersionTag.Type, out var versionTagId))
            {
                showtime.VersionTagId = versionTagId;
            }

            if (playtimeDict.TryGetValue(LookupKey, out var playtimeId))
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

    public async Task<List<JoinEvent>> GetAllAsync()
    {
        var joinEvents = await context
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
            .ToListAsync();

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
