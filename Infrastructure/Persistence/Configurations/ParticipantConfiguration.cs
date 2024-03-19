namespace Infrastructure.Persistence.Configurations;

public class ParticipantConfiguration : IEntityTypeConfiguration<Participant>
{
    public void Configure(EntityTypeBuilder<Participant> builder)
    {
        builder.HasMany(p => p.VotedFor).WithOne().HasForeignKey(pv => pv.ParticipantId);
    }
}
