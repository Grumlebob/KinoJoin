namespace Application.Interfaces;

/// <summary>
/// Handles getting calender information for a joinEvent and a particular showtime
/// </summary>
public interface ICalendarService
{
    /// <returns>A .ics file as a DotNetStreamReference, describing the event</returns>
    ///<remarks>The description of the calendar event includes the showtime's title,
    /// room name and version </remarks>
    public DotNetStreamReference GetCalendarFileStream(JoinEvent joinEvent, Showtime showtime);

    /// <returns>A Google Calendar URL for creating a new calendar event</returns>
    public Uri GetGoogleCalendarUrl(JoinEvent joinEvent, Showtime showtime);
}
