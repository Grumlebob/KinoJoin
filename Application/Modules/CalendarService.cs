using Domain;

namespace Application.Modules;

public class CalendarService : ICalendarService
{
    public DotNetStreamReference GenerateCalendarFileStream(JoinEvent joinEvent, Showtime showtime)
    {
        //We used this thread to help us with how to do it https://stackoverflow.com/questions/46033843/how-to-create-ics-file-using-c
        var startTime = showtime.Playtime.StartTime;
        var endTime = showtime.Playtime.StartTime.AddMinutes(showtime.Movie.DurationInMinutes);
        var summary = joinEvent.Title;
        var location = showtime.Cinema.Name;
        var description =
            joinEvent.Description
            + $"\n Film: {showtime.Movie.Title}, {showtime.Room.Name}, {showtime.VersionTag.Type}";

        var calenderStringBuilder = new StringBuilder();

        //start the calendar item
        calenderStringBuilder.AppendLine("BEGIN:VCALENDAR");
        calenderStringBuilder.AppendLine("VERSION:2.0");
        calenderStringBuilder.AppendLine("PRODID:stackoverflow.com");
        calenderStringBuilder.AppendLine("CALSCALE:GREGORIAN");
        calenderStringBuilder.AppendLine("METHOD:PUBLISH");

        //create a time zone if needed, TZID to be used in the event itself
        calenderStringBuilder.AppendLine("BEGIN:VTIMEZONE");
        calenderStringBuilder.AppendLine("TZID:Europe/Amsterdam");
        calenderStringBuilder.AppendLine("BEGIN:STANDARD");
        calenderStringBuilder.AppendLine("TZOFFSETTO:+0100");
        calenderStringBuilder.AppendLine("TZOFFSETFROM:+0100");
        calenderStringBuilder.AppendLine("END:STANDARD");
        calenderStringBuilder.AppendLine("END:VTIMEZONE");

        //add the event
        calenderStringBuilder.AppendLine("BEGIN:VEVENT");

        //with time zone specified
        calenderStringBuilder.AppendLine(
            "DTSTART;TZID=Europe/Amsterdam:" + startTime.ToString("yyyyMMddTHHmm00")
        );
        calenderStringBuilder.AppendLine(
            "DTEND;TZID=Europe/Amsterdam:" + endTime.ToString("yyyyMMddTHHmm00")
        );
        //or without
        calenderStringBuilder.AppendLine("DTSTART:" + startTime.ToString("yyyyMMddTHHmm00"));
        calenderStringBuilder.AppendLine("DTEND:" + endTime.ToString("yyyyMMddTHHmm00"));

        calenderStringBuilder.AppendLine("SUMMARY:" + summary + "");
        calenderStringBuilder.AppendLine("LOCATION:" + location + "");
        calenderStringBuilder.AppendLine("DESCRIPTION:" + description + "");
        calenderStringBuilder.AppendLine("PRIORITY:3");
        calenderStringBuilder.AppendLine("END:VEVENT");

        //end calendar item
        calenderStringBuilder.AppendLine("END:VCALENDAR");

        var calendarString = calenderStringBuilder.ToString();

        var bytes = Encoding.UTF8.GetBytes(calendarString);
        var stream = new MemoryStream(bytes);
        return new DotNetStreamReference(stream);
    }

    public Uri GenerateGoogleCalendarUrl(JoinEvent joinEvent, Showtime showtime)
    {
        return new Uri(
            "https://calendar.google.com/calendar/u/0/r/eventedit?"
                + $"text={joinEvent.Title}"
                + $"&dates={showtime.Playtime.StartTime:yyyyMMddTHHmmss}/{showtime.Playtime.StartTime.AddMinutes(showtime.Movie.DurationInMinutes):yyyyMMddTHHmmss}"
                + $"&location={showtime.Cinema.Name}"
                + $"&details={joinEvent.Description} %0AFilm: {showtime.Movie.Title}, {showtime.Room.Name}, {showtime.VersionTag.Type}"
        );
    }
}
