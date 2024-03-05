namespace Infrastructure.Database;

public class MonkeyContext : DbContext
{
    public DbSet<Monkey> Monkeys { get; set; }

    public MonkeyContext(DbContextOptions<MonkeyContext> options)
        : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new MonkeyConfiguration());
    }
}
