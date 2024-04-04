namespace Application.Interfaces;

public interface IFetchNewestKinoDkDataService
{
    /// <summary>
    /// Fetch all the static information from kino.dk (movies, cinemas etc). <br/>
    /// This enables the program to work independently without being redirected from kino.dk. <br/>
    /// This enables the frontpage to have working filters fetched from own database. <br/>
    /// This also makes end to end testing possible without relying on kino.dk to be the starting point.
    /// </summary>
    /// <param name="lowestCinemaId">First cinema to start from</param>
    /// <param name="highestCinemaId">Last cinema to include</param>
    /// <remarks>The purpose of lowestCinemaId and highestCinemaId is that in testing we can avoid loading all data, to save time.</remarks>
    Task UpdateBaseDataFromKinoDk(int lowestCinemaId, int highestCinemaId);
}
