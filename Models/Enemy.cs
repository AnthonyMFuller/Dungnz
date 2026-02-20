namespace TextGame.Models;

public abstract class Enemy
{
    public string Name { get; set; } = string.Empty;
    public int HP { get; set; }
    public int MaxHP { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int XPValue { get; set; }
    public LootTable LootTable { get; set; } = null!;
}
