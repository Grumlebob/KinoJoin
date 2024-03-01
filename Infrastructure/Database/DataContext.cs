namespace Infrastructure.Database;

public class DataContext : DbContext
{
    public DbSet<Monkey> Monkeys { get; set; }

    public DataContext(DbContextOptions<DataContext> options)
        : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new MonkeyConfiguration());
    }
}
