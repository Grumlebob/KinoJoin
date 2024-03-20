namespace Infrastructure.Persistence.Configurations;

public class PlaytimeConfiguration : IEntityTypeConfiguration<Playtime>
{
    public void Configure(EntityTypeBuilder<Playtime> builder)
    {

        builder.Property(p => p.StartTime)
            .HasConversion(d => d.ToUniversalTime()
                , d => d.ToLocalTime());
    }
}