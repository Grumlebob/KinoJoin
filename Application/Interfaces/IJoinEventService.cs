namespace Application.Interfaces;

public interface IJoinEventService
{
    Task<int> UpsertJoinEventAsync(UpsertJoinEventDto joinEventDto);
    Task<JoinEvent?> GetAsync(int id);
}
