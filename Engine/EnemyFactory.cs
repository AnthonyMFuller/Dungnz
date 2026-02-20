namespace Dungnz.Engine;

using Dungnz.Models;
using Dungnz.Systems.Enemies;

public static class EnemyFactory
{
    public static Enemy CreateRandom(Random rng)
    {
        var type = rng.Next(4);
        return type switch
        {
            0 => new Goblin(),
            1 => new Skeleton(),
            2 => new Troll(),
            _ => new DarkKnight()
        };
    }

    public static Enemy CreateBoss(Random rng)
    {
        return new DungeonBoss();
    }
}
