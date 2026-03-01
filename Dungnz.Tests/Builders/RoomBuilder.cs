using Dungnz.Models;

namespace Dungnz.Tests.Builders;

/// <summary>
/// Fluent builder for creating <see cref="Room"/> instances in tests.
/// </summary>
public class RoomBuilder
{
    private string _description = "A test chamber.";
    private RoomType _type = RoomType.Standard;
    private Enemy? _enemy;
    private readonly List<Item> _items = new();
    private bool _isExit;
    private bool _hasShrine;
    private readonly Dictionary<Direction, Room> _exits = new();

    public RoomBuilder Named(string description) { _description = description; return this; }
    public RoomBuilder OfType(RoomType type) { _type = type; return this; }
    public RoomBuilder WithEnemy(Enemy enemy) { _enemy = enemy; return this; }
    public RoomBuilder WithLoot(Item item) { _items.Add(item); return this; }
    public RoomBuilder AsExit() { _isExit = true; return this; }
    public RoomBuilder WithShrine() { _hasShrine = true; return this; }
    public RoomBuilder WithExit(Direction dir, Room room) { _exits[dir] = room; return this; }

    public Room Build()
    {
        var room = new Room
        {
            Description = _description,
            Type = _type,
            Enemy = _enemy,
            IsExit = _isExit,
            HasShrine = _hasShrine
        };
        foreach (var item in _items)
            room.Items.Add(item);
        foreach (var (dir, target) in _exits)
            room.Exits[dir] = target;
        return room;
    }
}
