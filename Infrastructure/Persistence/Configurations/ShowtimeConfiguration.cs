namespace Infrastructure.Persistence.Configurations;

public class ShowtimeConfiguration : IEntityTypeConfiguration<Showtime>
{
    public void Configure(EntityTypeBuilder<Showtime> builder)
    {
        builder.HasMany<ParticipantVote>().WithOne().HasForeignKey(v => v.ShowtimeId);
    }
}
