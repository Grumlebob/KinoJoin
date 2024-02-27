namespace Infrastructure.Database.Configuration;

public class MonkeyConfiguration : IEntityTypeConfiguration<Monkey>
{
    public void Configure(EntityTypeBuilder<Monkey> builder)
    {
        // Age is between 0 and 100
        builder.HasKey(m => m.Id);
    }
}
