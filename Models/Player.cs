namespace Dungnz.Models;

public class Player
{
    public string Name { get; set; } = string.Empty;
    public int HP { get; set; } = 100;
    public int MaxHP { get; set; } = 100;
    public int Attack { get; set; } = 10;
    public int Defense { get; set; } = 5;
    public int Gold { get; set; }
    public int XP { get; set; }
    public int Level { get; set; } = 1;
    public List<Item> Inventory { get; set; } = new();
}
