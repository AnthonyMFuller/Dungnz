namespace Dungnz.Models;

public class Room
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Description { get; set; } = string.Empty;
    public Dictionary<Direction, Room> Exits { get; set; } = new();
    public Enemy? Enemy { get; set; }
    public List<Item> Items { get; set; } = new();
    public bool IsExit { get; set; }
    public bool Visited { get; set; }
    public bool Looted { get; set; }
    public bool HasShrine { get; set; }
    public bool ShrineUsed { get; set; }
}
