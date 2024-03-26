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
    public required ShowtimeApiContentLevel1 ShowtimeApiContent { get; set; }
}

public class ShowtimeApiContentLevel1
{
    [JsonProperty("content")]
    public required ShowtimeApiContentLevel2 ShowtimeApiContent { get; set; }

    [JsonProperty("facets")]
    public required ShowtimeApiFacets ShowtimeApiFacets { get; set; }
}

public class ShowtimeApiContentLevel2
{
    [JsonProperty("content")]
    public required List<ShowtimeApiContentLevel3> Content { get; set; }
}

public class ShowtimeApiContentLevel3
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("movies")]
    public required List<ShowtimeApiMovie> Movies { get; set; }
}

public class ShowtimeApiMovie
{
    [JsonProperty("id")]
    public int Id { get; init; }

    [JsonProperty("versions")]
    public required List<ShowtimeApiVersion> Versions { get; set; }

    [JsonProperty("content")]
    public required ShowtimeApiMovieContent Content { get; set; }
}

public class ShowtimeApiVersion
{
    [JsonProperty("label")]
    public required string Label { get; set; }

    [JsonProperty("dates")]
    public required List<ShowtimeApiDate> Dates { get; set; }
}

public class ShowtimeApiDate
{
    [JsonProperty("date")]
    public required string Date { get; set; }

    [JsonProperty("showtimes")]
    public required List<ShowtimeApiItem> Showtimes { get; set; }
}

public class ShowtimeApiItem
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("available_seats")]
    public int AvailableSeats { get; set; }

    [JsonProperty("time")]
    public required string Time { get; set; }

    [JsonProperty("room")]
    public required ShowtimeApiRoomContent ShowtimeApiRoomContent { get; set; }
}

public class ShowtimeApiRoomContent
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("label")]
    public required string Label { get; set; }
}

public class ShowtimeApiMovieContent
{
    [JsonProperty("field_censorship_icon")]
    public string? FieldCensorshipIcon { get; set; }

    [JsonProperty("field_playing_time")]
    public required string FieldPlayingTime { get; set; }

    [JsonProperty("field_poster")]
    public required ShowtimeApiFieldPoster ShowtimeApiFieldPoster { get; set; }

    [JsonProperty("field_premiere")]
    public required string FieldPremiere { get; set; }

    [JsonProperty("url")]
    public required string Url { get; set; }
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
    public ShowtimeApiFieldMediaImage? FieldMediaImage { get; set; }
}

public class ShowtimeApiFieldMediaImage
{
    [JsonProperty("sources")]
    public List<ShowtimeApiSourceItem>? Sources { get; set; }
}

public class ShowtimeApiSourceItem
{
    [JsonProperty("srcset")]
    public string? Srcset { get; set; }
}

public class ShowtimeApiCinemas
{
    [JsonProperty("options")]
    public required List<ShowtimeApiCinemaOption> Options { get; set; }
}

public class ShowtimeApiCinemaOption
{
    [JsonProperty("key")]
    public int Key { get; set; }

    [JsonProperty("value")]
    public required string Value { get; set; }
}

public class ShowtimeApiFacets
{
    [JsonProperty("cinemas")]
    public required ShowtimeApiCinemas ShowtimeApiCinemas { get; set; }

    [JsonProperty("movies")]
    public required MovieLookup Movies { get; set; }

    [JsonProperty("versions")]
    public required VersionLookup Versions { get; set; }

    [JsonProperty("genres")]
    public required ShowtimeApiGenresLookup ShowtimeApiGenres { get; set; }
}

public class FacetOptionVersion
{
    [JsonProperty("value")]
    public required string Value { get; set; }
}

public class FacetOption
{
    [JsonProperty("key")]
    public required int Key { get; set; }

    [JsonProperty("value")]
    public required string Value { get; set; }
}

public class ShowtimeApiGenresLookup
{
    [JsonProperty("options")]
    public required List<FacetOption> Options { get; set; }
}

public class MovieLookup
{
    [JsonProperty("options")]
    public required List<FacetOption> Options { get; set; }
}

public class VersionLookup
{
    [JsonProperty("options")]
    public required List<FacetOptionVersion> Options { get; set; }
}
