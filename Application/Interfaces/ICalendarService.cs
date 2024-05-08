using Domain;

namespace Application.Interfaces;

/// <summary>
/// Handles generating calender information for a joinEvent and a particular showtime
/// </summary>
public interface ICalendarService
{
    /// <returns>A .ics file as a DotNetStreamReference, describing the event</returns>
    ///<remarks>The description of the calendar event includes the showtime's title,
    /// room name and version </remarks>
    public DotNetStreamReference GenerateCalendarFileStream(JoinEvent joinEvent, Showtime showtime);

    /// <returns>A Google Calendar URL for creating a new calendar event</returns>
    public Uri GenerateGoogleCalendarUrl(JoinEvent joinEvent, Showtime showtime);
}
