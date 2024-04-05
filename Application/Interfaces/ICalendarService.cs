using Microsoft.JSInterop;

namespace Application.Interfaces;

public interface ICalendarService
{
    public string GetCalendarFileString(JoinEvent joinEvent, Showtime showtime);
    public DotNetStreamReference GetCalendarFileStream(JoinEvent joinEvent, Showtime showtime);
    public Uri GetGoogleCalendarUrl(JoinEvent joinEvent, Showtime showtime);
}
