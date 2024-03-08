namespace Application.Interfaces;

public interface IJoinEventService
{
    Task<int> PutAsync(UpsertJoinEventDto joinEventDto);
    Task<JoinEvent?> GetAsync(int id);
}
