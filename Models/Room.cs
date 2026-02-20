namespace Dungnz.Models;

public class Room
{
    public string Description { get; set; } = string.Empty;
    public Dictionary<Direction, Room> Exits { get; set; } = new();
    public Enemy? Enemy { get; set; }
    public List<Item> Items { get; set; } = new();
    public bool IsExit { get; set; }
    public bool Visited { get; set; }
    public bool Looted { get; set; }
}
