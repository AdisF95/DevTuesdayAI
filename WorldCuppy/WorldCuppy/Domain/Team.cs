namespace WorldCuppy.Domain;

public class Team
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
}
