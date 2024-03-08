using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Infrastructure.Services;

public class JoinEventService(KinoContext context) : IJoinEventService
{
    public async Task<int> PutAsync(UpsertJoinEventDto joinEventDto)
    {
        var joinEventWithNavProps = UpsertJoinEventDto.FromUpsertDtoToModel(joinEventDto);

        //make sure all entities exist in database
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
            foreach (var showtime in joinEventWithNavProps.Showtimes)
            {
                showtime.PlaytimeId = (
                    await context.Playtimes.FirstAsync(p =>
                        p.StartTime == showtime.Playtime.StartTime
                    )
                ).Id;
                showtime.VersionTagId = (
                    await context.Versions.FirstAsync(v => v.Type == showtime.VersionTag.Type)
                ).Id;
            }

            await context.Showtimes.UpsertRange(joinEventWithNavProps.Showtimes).RunAsync();
            await context.SaveChangesAsync();

            joinEventWithNavProps.Host = null!; // avoid creating a new host

            //As mulitple showtimes can reference the same movie etc, we must only attach it once in ef core
            var showtimeIds = joinEventDto.Showtimes.Select(s => s.Id);
            var voteOptions = joinEventDto.SelectOptions.Select(s => s.VoteOption);
            var colorOptions = joinEventDto.SelectOptions.Select(s => s.Color);

            EntityEntry<JoinEvent> newlyUpsertedJoinEvent; //is set by add or update
            if (joinEventDto.Id == null) //new event
            {
                //use existing showtimes etc
                joinEventWithNavProps.Showtimes = context
                    .Showtimes.Where(s => showtimeIds.Contains(s.Id))
                    .ToList();
                joinEventWithNavProps.SelectOptions = context
                    .SelectOptions.Where(s =>
                        voteOptions.Contains(s.VoteOption) && colorOptions.Contains(s.Color)
                    )
                    .ToList();
                newlyUpsertedJoinEvent = await context.JoinEvents.AddAsync(joinEventWithNavProps);
            }
            else
            {
                //dont add already existing entities
                joinEventWithNavProps.Participants
                    .RemoveAll(p => p.Id != 0); //they are 0 if yet to be added to database
                joinEventWithNavProps.SelectOptions.RemoveAll(s =>
                    voteOptions.Contains(s.VoteOption) && colorOptions.Contains(s.Color)
                );
                joinEventWithNavProps.Showtimes = []; //these cannot change after joinEvent is created

                newlyUpsertedJoinEvent = context.JoinEvents.Update(joinEventWithNavProps);
            }

            await context.SaveChangesAsync();
            return newlyUpsertedJoinEvent.Entity.Id;
        }
        catch (Exception _) //If not all data was found in the preseeded database, add it and try again
        {
            var movies = joinEventWithNavProps
                .Showtimes.Select(st => st.Movie)
                .DistinctBy(m => m.Id);
            await context.Movies.UpsertRange(movies).RunAsync();

            var cinemas = joinEventWithNavProps
                .Showtimes.Select(st => st.Cinema)
                .DistinctBy(c => c.Id);
            await context.Cinemas.UpsertRange(cinemas).RunAsync();

            var versions = joinEventWithNavProps
                .Showtimes.Select(st => st.VersionTag)
                .DistinctBy(v => v.Type);
            await context.Versions.UpsertRange(versions).On(v => v.Type).RunAsync();

            foreach (var showtime in joinEventWithNavProps.Showtimes)
            {
                showtime.PlaytimeId = (
                    await context.Playtimes.FirstAsync(p =>
                        p.StartTime == showtime.Playtime.StartTime
                    )
                ).Id;
                showtime.VersionTagId = (
                    await context.Versions.FirstAsync(v => v.Type == showtime.VersionTag.Type)
                ).Id;
            }

            await context.Showtimes.UpsertRange(joinEventWithNavProps.Showtimes).RunAsync();
            await context.SaveChangesAsync();

            joinEventWithNavProps.Host = null!; // avoid creating a new host
            EntityEntry<JoinEvent> newlyUpsertedJoinEvent; //is set by add or update
            var voteOptions = joinEventDto.SelectOptions.Select(s => s.VoteOption);
            var colorOptions = joinEventDto.SelectOptions.Select(s => s.Color);
            if (joinEventDto.Id == null) //new event
            {
                //As mulitple showtimes can reference the same movie etc, we must only attach it once in ef core
                var showtimeIds = joinEventDto.Showtimes.Select(s => s.Id);

                //use existing showtimes etc
                joinEventWithNavProps.Showtimes = context
                    .Showtimes.Where(s => showtimeIds.Contains(s.Id))
                    .ToList();
                joinEventWithNavProps.SelectOptions = context
                    .SelectOptions.Where(s =>
                        voteOptions.Contains(s.VoteOption) && colorOptions.Contains(s.Color)
                    )
                    .ToList();
                newlyUpsertedJoinEvent = await context.JoinEvents.AddAsync(joinEventWithNavProps);
            }
            else
            {
                //dont add already existing entities
                joinEventWithNavProps.Participants
                    .RemoveAll(p => p.Id != 0); //they are null if yet to be added to database
                joinEventWithNavProps.SelectOptions = context
                    .SelectOptions.Where(s =>
                        voteOptions.Contains(s.VoteOption) && colorOptions.Contains(s.Color)
                    )
                    .ToList();
                joinEventWithNavProps.Showtimes = []; //these cannot change after joinEvent is created

                newlyUpsertedJoinEvent = context.JoinEvents.Update(joinEventWithNavProps);
            }

            await context.SaveChangesAsync();
            return newlyUpsertedJoinEvent.Entity.Id;
        }
    }

    public async Task<JoinEvent?> GetAsync(int id)
    {
        var result = await context.JoinEvents
            .AsNoTracking()
            .Include(j => j.Showtimes).ThenInclude(s => s.Movie)
            .Include(j => j.Showtimes).ThenInclude(s => s.Cinema)
            .Include(j => j.Showtimes).ThenInclude(s => s.Playtime)
            .Include(j => j.Showtimes).ThenInclude(s => s.VersionTag)
            .Include(j => j.Showtimes).ThenInclude(s => s.Room)
            .Include(j => j.Participants).ThenInclude(p => p.VotedFor)
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