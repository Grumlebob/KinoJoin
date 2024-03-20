namespace Application.Interfaces;

public interface IFetchNewestKinoDkDataService
{
    Task UpdateBaseDataFromKinoDk(int lowestCinemaId, int highestCinemaId);
}
