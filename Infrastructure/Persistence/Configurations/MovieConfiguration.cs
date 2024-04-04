namespace Infrastructure.Persistence.Configurations;

public class MovieConfiguration : IEntityTypeConfiguration<Movie>
{
    public void Configure(EntityTypeBuilder<Movie> builder)
    {
        //Make sure substring that is in every image url is not stored in the database
        builder
            .Property(m => m.ImageUrl)
            .HasConversion(url => url.Replace(
                "https://api.kino.dk/sites/kino.dk/files/styles/isg_focal_point_356_534/public/",
                ""), url => "https://api.kino.dk/sites/kino.dk/files/styles/isg_focal_point_356_534/public/" + url);
    }
}