namespace Domain.Entities;

public class Monkey
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public required string Name { get; set; }
    public required int Age { get; set; }
}
