using Newtonsoft.Json.Linq;

namespace Domain.ExternalApiModels;

// ReSharper disable InvalidXmlDocComment
/// <summary>
/// Used to parse API response data from https://kino.dk/ticketflow/showtimes
/// The actual api is: https://api.kino.dk/ticketflow/showtimes?FiltersGoesHere&region=content&format=json
/// Full example: https://api.kino.dk/ticketflow/showtimes?cinemas=53&cinemas=49&movies=35883&movies=76769&sort=most_purchased?region=content&format=json
/// </summary>
public class ShowtimeApiRoot
{
    [JsonProperty("content")]
    public required ShowtimeApiContentLevel1 ShowtimeApiContent;
}

public class ShowtimeApiContentLevel1
{
    [JsonProperty("content")]
    public required ShowtimeApiContentLevel2 ShowtimeApiContent;

    [JsonProperty("facets")]
    public required ShowtimeApiFacets ShowtimeApiFacets;
}

public class ShowtimeApiContentLevel2
{
    [JsonProperty("content")]
    public required List<ShowtimeApiContentLevel3> Content;
}

public class ShowtimeApiContentLevel3
{
    [JsonProperty("id")]
    public int Id;

    [JsonProperty("movies")]
    public required List<ShowtimeApiMovie> Movies;
}

public class ShowtimeApiMovie
{
    [JsonProperty("id")]
    public int Id;

    [JsonProperty("versions")]
    public required List<ShowtimeApiVersion> Versions;

    [JsonProperty("content")]
    public required ShowtimeApiMovieContent Content;

    [JsonProperty("type")]
    public string Type { get; set; } = "";
}

public class ShowtimeApiVersion
{
    [JsonProperty("label")]
    public required string Label;

    [JsonProperty("dates")]
    public required List<ShowtimeApiDate> Dates;
}

public class ShowtimeApiDate
{
    [JsonProperty("date")]
    public required string Date;

    [JsonProperty("showtimes")]
    public required List<ShowtimeApiItem> Showtimes;
}

public class ShowtimeApiItem
{
    [JsonProperty("id")]
    public int Id;

    [JsonProperty("available_seats")]
    public int AvailableSeats;

    [JsonProperty("time")]
    public required string Time;

    [JsonProperty("room")]
    public required ShowtimeApiRoomContent ShowtimeApiRoomContent;
}

public class ShowtimeApiRoomContent
{
    [JsonProperty("id")]
    public int Id;

    [JsonProperty("label")]
    public required string Label;
}

public class ShowtimeApiMovieContent
{
    [JsonProperty("field_censorship_icon")]
    public string? FieldCensorshipIcon;

    [JsonProperty("label")]
    public string Label { get; set; } = "";

    [JsonProperty("field_playing_time")]
    public required string FieldPlayingTime;

    [JsonProperty("field_poster")]
    public required ShowtimeApiFieldPoster ShowtimeApiFieldPoster;

    [JsonProperty("field_premiere")]
    public required string FieldPremiere;

    [JsonProperty("url")]
    public required string Url;
}

/// <summary>
/// Since their data sometimes has a list of images, and sometimes just a single image, we need to handle both cases.
/// eg.
/// "field_media_image": { }
/// "field_media_image": [ ]
/// </summary>
public class FieldMediaImageConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return (objectType == typeof(ShowtimeApiFieldMediaImage));
    }

    public override object ReadJson(
        JsonReader reader,
        Type objectType,
        object? existingValue,
        JsonSerializer serializer
    )
    {
        JToken token = JToken.Load(reader);
        if (token.Type == JTokenType.Array)
        {
            return null!;
        }

        return token.ToObject<ShowtimeApiFieldMediaImage>()!;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new Exception("Only implemented to satisfy the interface. To make a converter");
    }
}

public class ShowtimeApiFieldPoster
{
    [JsonProperty("field_media_image"), JsonConverter(typeof(FieldMediaImageConverter))]
    public ShowtimeApiFieldMediaImage? FieldMediaImage;
}

public class ShowtimeApiFieldMediaImage
{
    [JsonProperty("sources")]
    public List<ShowtimeApiSourceItem>? Sources;
}

public class ShowtimeApiSourceItem
{
    [JsonProperty("srcset")]
    public string? Srcset;
}

public class ShowtimeApiCinemas
{
    [JsonProperty("options")]
    public required List<ShowtimeApiCinemaOption> Options;
}

public class ShowtimeApiCinemaOption
{
    [JsonProperty("key")]
    public int Key;

    [JsonProperty("value")]
    public required string Value;
}

public class ShowtimeApiFacets
{
    [JsonProperty("cinemas")]
    public required ShowtimeApiCinemas ShowtimeApiCinemas;

    [JsonProperty("movies")]
    public required MovieLookup Movies;

    [JsonProperty("versions")]
    public required VersionLookup Versions;

    [JsonProperty("genres")]
    public required ShowtimeApiGenresLookup ShowtimeApiGenres;
}

public class FacetOptionVersion
{
    [JsonProperty("value")]
    public required string Value;
}

public class FacetOption
{
    [JsonProperty("key")]
    public required int Key;

    [JsonProperty("value")]
    public required string Value;
}

public class ShowtimeApiGenresLookup
{
    [JsonProperty("options")]
    public required List<FacetOption> Options;
}

public class MovieLookup
{
    [JsonProperty("options")]
    public required List<FacetOption> Options;
}

public class VersionLookup
{
    [JsonProperty("options")]
    public required List<FacetOptionVersion> Options;
}
