using Newtonsoft.Json;

namespace Domain.ExternalApi;


    public class Root
    {
        [JsonProperty("content")] public ContentLevel1 Content { get; set; }
    }

    public class ContentLevel1
    {
        [JsonProperty("content")] public ContentLevel2 Content { get; set; }
    }

    public class ContentLevel2
    {
        [JsonProperty("content")] public ContentLevel3 Content { get; set; }

        [JsonProperty("facets")] public Facets Facets { get; set; }
    }


    public class ContentLevel3
    {
        [JsonProperty("content")] public List<ContentLevel4> Content { get; set; }
    }

    public class ContentLevel4
    {
        [JsonProperty("type")] public string Type { get; set; }

        [JsonProperty("id")] public int Id { get; set; }

        [JsonProperty("movies")] public List<MovieJson> Movies { get; set; }
    }

    public class MovieJson
    {
        [JsonProperty("type")] public string Type { get; set; }

        [JsonProperty("id")] public int Id { get; set; }

        [JsonProperty("versions")] public List<Version> Versions { get; set; }

        [JsonProperty("content")] public MovieContent Content { get; set; }
    }

    public class Version
    {
        [JsonProperty("type")] public string Type { get; set; }

        [JsonProperty("label")] public string Label { get; set; }

        [JsonProperty("dates")] public List<ShowtimeDate> Dates { get; set; }
    }

    public class ShowtimeDate
    {
        [JsonProperty("type")] public string Type { get; set; }

        [JsonProperty("date")] public string Date { get; set; }

        [JsonProperty("showtimes")] public List<ShowtimeItem> Showtimes { get; set; }
    }

    public class ShowtimeItem
    {
        [JsonProperty("type")] public string Type { get; set; }

        [JsonProperty("id")] public int Id { get; set; }

        [JsonProperty("available_seats")] public int AvailableSeats { get; set; }

        [JsonProperty("time")] public string Time { get; set; }

        [JsonProperty("room")] public Room Room { get; set; }
    }

    public class Room
    {
        [JsonProperty("type")] public string Type { get; set; }

        [JsonProperty("id")] public int Id { get; set; }

        [JsonProperty("label")] public string Label { get; set; }
    }

    public class MovieContent
    {
        [JsonProperty("type")] public string Type { get; set; }

        [JsonProperty("id")] public string Id { get; set; }

        [JsonProperty("label")] public string Label { get; set; }

        [JsonProperty("field_censorship_icon")]
        public string FieldCensorshipIcon { get; set; }

        [JsonProperty("field_playing_time")] public string FieldPlayingTime { get; set; }

        [JsonProperty("field_poster")] public FieldPoster FieldPoster { get; set; }

        [JsonProperty("field_premiere")] public string FieldPremiere { get; set; }
        
        [JsonProperty("url")] public string? URL { get; set; }
    }

    public class FieldPoster
    {
        [JsonProperty("type")] public string Type { get; set; }
        [JsonProperty("field_media_image")] public FieldMediaImage FieldMediaImage { get; set; }
    }

    public class FieldMediaImage
    {
        [JsonProperty("type")] public string Type { get; set; }

        [JsonProperty("responsive_image_style_id")]
        public string ResponsiveImageStyleId { get; set; }

        [JsonProperty("width")] public int Width { get; set; }

        [JsonProperty("height")] public int Height { get; set; }

        [JsonProperty("sources")] public List<SourceItem> Sources { get; set; } // Added this line
    }

    public class SourceItem
    {
        [JsonProperty("srcset")] public string Srcset { get; set; }

        [JsonProperty("media")] public string Media { get; set; }

        [JsonProperty("type")] public string Type { get; set; }

        [JsonProperty("width")] public int? Width { get; set; } // Nullable because it might not always be present

        [JsonProperty("height")] public int? Height { get; set; }
    }

    public class Cinemas
    {
        [JsonProperty("type")] public string Type { get; set; }

        [JsonProperty("title")] public string Title { get; set; }

        //ignore this json
        [JsonIgnore]
        [JsonProperty("default_value")]
        public string DefaultValue { get; set; }

        [JsonProperty("options")] public List<CinemaOption> Options { get; set; }
    }

    public class CinemaOption
    {
        [JsonProperty("key")] public int Key { get; set; }

        [JsonProperty("value")] public string Value { get; set; }
    }

    public class Advertisement
    {
    }

    public class Breadcrumbs
    {
    }


    public class Facets
    {
        [JsonProperty("sort")] public object Sort { get; set; } // Replace object with specific type if necessary

        [JsonProperty("city")] public object City { get; set; } // Replace object with specific type if necessary

        [JsonProperty("cinemas")] public Cinemas Cinemas { get; set; }

        [JsonProperty("movies")] public MovieLookup Movies { get; set; } // Replace object with specific type if necessary

        [JsonProperty("versions")] public object Versions { get; set; } // Replace object with specific type if necessary

        [JsonProperty("genres")] public GenresLookup Genres { get; set; } // Replace object with specific type if necessary

        [JsonProperty("date")] public object Date { get; set; } // Replace object with specific type if necessary
    }

    public class GenresLookup
    {
        [JsonProperty("options")] public List<GenrerOptions> Options { get; set; }
    }

    public class GenrerOptions
    {
        [JsonProperty("key")] public int Key { get; set; }

        [JsonProperty("value")] public string Value { get; set; }
    }


    public class MovieLookup
    {
        [JsonProperty("options")] public List<MovieOptions> Options { get; set; }
    }

    public class MovieOptions
    {
        [JsonProperty("key")] public int Key { get; set; }

        [JsonProperty("value")] public string Value { get; set; }
    }

    public class Footer
    {
    }

    public class Header
    {
    }

    public class Initial
    {
    }

    public class Pager
    {
    }


    public class Universe
    {
    }