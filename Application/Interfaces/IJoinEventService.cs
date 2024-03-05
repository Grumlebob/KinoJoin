namespace Application.Interfaces;

public interface IJoinEventService
{
    Task<int> PutAsync(JoinEvent joinEvent);
}