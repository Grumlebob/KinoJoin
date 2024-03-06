namespace Infrastructure.Services;

public class JoinEventService(KinoContext context) : IJoinEventService
{
    public async Task<int> PutAsync(UpsertJoinEventDto joinEventDto)
    {
        var joinEventWithNavProps = UpsertJoinEventDto.FromUpsertDtoToModel(joinEventDto);
        //make sure all entities exist in database 
        await context.Hosts.Upsert(joinEventWithNavProps.Host).RunAsync();

        await context.SelectOptions.UpsertRange(joinEventWithNavProps.SelectOptions).On(s => new { s.VoteOption, s.Color })
            .RunAsync();

        var cinemas = joinEventWithNavProps.Showtimes.Select(st => st.Cinema).DistinctBy(c => c.Id);
        await context.Cinemas.UpsertRange(cinemas).RunAsync();

        var movies = joinEventWithNavProps.Showtimes.Select(st => st.Movie).DistinctBy(m => m.Id);
        await context.Movies.UpsertRange(movies).RunAsync();

        var playtimes = joinEventWithNavProps.Showtimes.Select(st => st.Playtime).DistinctBy(p => p.StartTime);
        await context.Playtimes.UpsertRange(playtimes).On(p => p.StartTime).RunAsync();

        var versions = joinEventWithNavProps.Showtimes.Select(st => st.VersionTag).DistinctBy(v => v.Type);
        await context.Versions.UpsertRange(versions).On(v => v.Type).RunAsync();

        var rooms = joinEventWithNavProps.Showtimes.Select(st => st.Room).DistinctBy(r => r.Id);
        await context.Rooms.UpsertRange(rooms).RunAsync();

        await context.SaveChangesAsync();

        await context.Showtimes.UpsertRange(joinEventWithNavProps.Showtimes).RunAsync();
        await context.SaveChangesAsync();

        if (joinEventDto.Id == null)
        {
            var newlyAddedJoinEvent = await context.JoinEvents.AddAsync(joinEventWithNavProps);
            await context.SaveChangesAsync();
            return newlyAddedJoinEvent.Entity.Id;
        }

        //Add joinEvent with existing entities
        /*var showtimeIds = joinEventDto.Showtimes.Select(s => s.Id);
        var voteOptions = joinEventDto.SelectOptions.Select(s => s.VoteOption);
        var colorOptions = joinEventDto.SelectOptions.Select(s => s.Color);

        var newJoinEvent = new JoinEvent
        {
            Title = joinEventDto.Title,
            Description = joinEventDto.Description,
            HostId = joinEventDto.Host.AuthId,
            Deadline = joinEventDto.Deadline,
            Participants = joinEventDto.Participants,
            Showtimes = context.Showtimes.Where(s => showtimeIds.Contains(s.Id)).ToList(),
            SelectOptions = context.SelectOptions
                .Where(s => voteOptions.Contains(s.VoteOption) && colorOptions.Contains(s.Color))
                .ToList(),
            ChosenShowtimeId = joinEventDto.ChosenShowtimeId
        };*/

        var newlyUpdatedJoinEvent = context.JoinEvents.Update(joinEventWithNavProps);
        await context.SaveChangesAsync();

        return newlyUpdatedJoinEvent.Entity.Id;
    }
}