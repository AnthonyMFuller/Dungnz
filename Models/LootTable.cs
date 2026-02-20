namespace TextGame.Models;

public class LootTable
{
    private readonly List<(Item item, double chance)> _drops = new();
    private readonly int _minGold;
    private readonly int _maxGold;
    private readonly Random _rng;
    
    public LootTable(Random? rng = null, int minGold = 0, int maxGold = 0)
    {
        _rng = rng ?? new Random();
        _minGold = minGold;
        _maxGold = maxGold;
    }
    
    public void AddDrop(Item item, double chance)
    {
        _drops.Add((item, chance));
    }
    
    public LootResult RollDrop(Enemy enemy)
    {
        int gold = _minGold == _maxGold ? _minGold : _rng.Next(_minGold, _maxGold + 1);
        
        Item? dropped = null;
        foreach (var (item, chance) in _drops)
        {
            if (_rng.NextDouble() < chance)
            {
                dropped = item;
                break;
            }
        }
        
        return new LootResult { Item = dropped, Gold = gold };
    }
}
