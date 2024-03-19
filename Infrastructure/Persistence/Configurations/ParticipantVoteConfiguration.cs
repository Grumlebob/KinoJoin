namespace Infrastructure.Persistence.Configurations;

public class ParticipantVoteConfiguration : IEntityTypeConfiguration<ParticipantVote>
{
    public void Configure(EntityTypeBuilder<ParticipantVote> builder)
    {
        builder.HasKey(pv => new { pv.ParticipantId, pv.ShowtimeId });
    }
}
