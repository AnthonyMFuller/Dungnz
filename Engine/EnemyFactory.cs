namespace TextGame.Engine;

using TextGame.Models;

// Stub enemy classes until Barton delivers full implementations in Systems/Enemies/
internal class GoblinStub : Enemy
{
    public GoblinStub()
    {
        Name = "Goblin";
        HP = MaxHP = 20;
        Attack = 8;
        Defense = 2;
        XPValue = 15;
        LootTable = new LootTable();
    }
}

internal class SkeletonStub : Enemy
{
    public SkeletonStub()
    {
        Name = "Skeleton";
        HP = MaxHP = 25;
        Attack = 10;
        Defense = 3;
        XPValue = 20;
        LootTable = new LootTable();
    }
}

internal class TrollStub : Enemy
{
    public TrollStub()
    {
        Name = "Troll";
        HP = MaxHP = 40;
        Attack = 12;
        Defense = 5;
        XPValue = 35;
        LootTable = new LootTable();
    }
}

internal class DarkKnightStub : Enemy
{
    public DarkKnightStub()
    {
        Name = "Dark Knight";
        HP = MaxHP = 50;
        Attack = 15;
        Defense = 8;
        XPValue = 50;
        LootTable = new LootTable();
    }
}

internal class DungeonBossStub : Enemy
{
    public DungeonBossStub()
    {
        Name = "Dungeon Boss";
        HP = MaxHP = 100;
        Attack = 20;
        Defense = 10;
        XPValue = 100;
        LootTable = new LootTable();
    }
}

public static class EnemyFactory
{
    public static Enemy CreateRandom(Random rng)
    {
        var type = rng.Next(4);
        return type switch
        {
            0 => new GoblinStub(),
            1 => new SkeletonStub(),
            2 => new TrollStub(),
            _ => new DarkKnightStub()
        };
    }

    public static Enemy CreateBoss(Random rng)
    {
        return new DungeonBossStub();
    }
}
