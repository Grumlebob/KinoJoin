namespace Infrastructure.Persistence.Configurations;

public class JoinEventConfiguration : IEntityTypeConfiguration<JoinEvent>
{
    public void Configure(EntityTypeBuilder<JoinEvent> builder)
    {
        //Many to Many relations
        builder.HasMany(je => je.Showtimes).WithMany();

        builder.HasMany(je => je.SelectOptions).WithMany();

        builder
            .HasOne(je => je.DefaultSelectOption)
            .WithMany()
            .HasForeignKey(je => je.DefaultSelectOptionId);

        // Many to one relations
        builder.HasMany(je => je.Participants).WithOne().HasForeignKey(p => p.JoinEventId);

        builder
            .Property(je => je.Deadline)
            .HasConversion(d => d.ToUniversalTime(), d => d.ToLocalTime());
    }
}
