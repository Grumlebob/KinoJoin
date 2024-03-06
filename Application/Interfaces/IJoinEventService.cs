namespace Application.Interfaces;

public interface IJoinEventService
{
    Task<int> PutAsync(UpsertJoinEventDto joinEventDto);
}