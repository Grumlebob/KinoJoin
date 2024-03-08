using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Infrastructure.Services;

public class JoinEventService(KinoContext context) : IJoinEventService
{
    public async Task<int> PutAsync(UpsertJoinEventDto joinEventDto)
    {
        var joinEventWithNavProps = UpsertJoinEventDto.FromUpsertDtoToModel(joinEventDto);

        //Upsert SelectOptions, Playtimes, Rooms and host: To make sure they exist in DB before JoinEvent is added
        await context.Hosts.Upsert(joinEventWithNavProps.Host).RunAsync();

        await context
            .SelectOptions.UpsertRange(joinEventWithNavProps.SelectOptions)
            .On(s => new { s.VoteOption, s.Color })
            .RunAsync();

        var playTimes = joinEventWithNavProps
            .Showtimes.Select(st => st.Playtime)
            .DistinctBy(p => p.StartTime);
        await context.Playtimes.UpsertRange(playTimes).On(p => p.StartTime).RunAsync();

        var newRooms = joinEventWithNavProps
            .Showtimes.Select(st => st.Room)
            .DistinctBy(r => r.Id)
            .ToList();
        await context.Rooms.UpsertRange(newRooms).RunAsync();
        await context.SaveChangesAsync();

        //Try to add JoinEvent using preseeded data in database
        try
        {
            return await HandleJoinEventUpsert(joinEventDto, joinEventWithNavProps);
        }
        catch (Exception _)
        {
            await UpsertMissingEntities(joinEventWithNavProps);
            return await HandleJoinEventUpsert(joinEventDto, joinEventWithNavProps);
        }
    }

    private async Task<int> HandleJoinEventUpsert(
        UpsertJoinEventDto joinEventDto,
        JoinEvent joinEventWithNavProps
    )
    {
        await AttachPlaytimeAndVersionId(joinEventWithNavProps.Showtimes);

        await context.Showtimes.UpsertRange(joinEventWithNavProps.Showtimes).RunAsync();
        await context.SaveChangesAsync();

        joinEventWithNavProps.Host = null!; // Avoid creating a new host

        bool isUpdate = joinEventDto.Id != null;
        await PrepareJoinEvent(joinEventDto, joinEventWithNavProps, isUpdate);

        EntityEntry<JoinEvent> newlyUpsertedJoinEvent = isUpdate
            ? context.JoinEvents.Update(joinEventWithNavProps)
            : await context.JoinEvents.AddAsync(joinEventWithNavProps);

        await context.SaveChangesAsync();
        return newlyUpsertedJoinEvent.Entity.Id;
    }

    private async Task PrepareJoinEvent(
        UpsertJoinEventDto joinEventDto,
        JoinEvent joinEventWithNavProps,
        bool isUpdate
    )
    {
        var voteOptions = joinEventDto.SelectOptions.Select(s => s.VoteOption);
        var colorOptions = joinEventDto.SelectOptions.Select(s => s.Color);

        if (isUpdate)
        {
            // Logic for updating JoinEvent
            joinEventWithNavProps.Participants.RemoveAll(p => p.Id != 0); // They are 0 if yet to be added to database
            joinEventWithNavProps.SelectOptions.RemoveAll(s =>
                voteOptions.Contains(s.VoteOption) && colorOptions.Contains(s.Color)
            );
            joinEventWithNavProps.Showtimes = []; // These cannot change after joinEvent is created
        }
        else
        {
            var showtimeIds = joinEventDto.Showtimes.Select(s => s.Id);
            // Logic for adding new JoinEvent
            joinEventWithNavProps.Showtimes = context
                .Showtimes.Where(s => showtimeIds.Contains(s.Id))
                .ToList();
            joinEventWithNavProps.SelectOptions = context
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
}
