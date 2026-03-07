namespace Dungnz.Systems;

/// <summary>
/// Flavor text pools for merchant encounters.
/// </summary>
public static class MerchantNarration
{
    private static readonly Random _rng = new();

    // Shown when entering a room with a merchant
    /// <summary>Lines shown when entering a room with a merchant.</summary>
    public static readonly string[] Greetings =
    {
        "A cloaked figure stands in the shadows. \"I heard you coming. Most don't.\"",
        "Something sits very still at a folding table. \"You look like you need something.\"",
        "\"Weapons or healing?\" The figure doesn't wait for an answer. \"Both, then.\"",
        "The smell of incense and old coin. Someone has been waiting for you here.",
        "\"I don't ask questions about the blood,\" it says. \"You shouldn't ask questions about me.\"",
        "A hooded shape crouches over a cloth-covered spread. It doesn't look up. \"Still alive, then.\"",
        "\"Funny place to meet,\" the figure murmurs. It doesn't sound like it finds it funny at all.",
        "The merchant's eyes catch the dark like a cat's. \"You're not the first today. The others didn't make it far.\"",
        "\"Everything has a price down here. I just happen to know what it is.\"",
        "A figure assembled from patience and shadow. \"Take your time. You probably won't.\"",
        "\"I've been waiting,\" it says, which should be reassuring. It isn't.",
        "The merchant doesn't greet you. Just watches, as if calculating something."
    };

    // Shown after a purchase
    /// <summary>Lines shown after the player completes a purchase.</summary>
    public static readonly string[] AfterPurchase =
    {
        "\"A pleasure. Probably.\"",
        "\"Come back if you live.\"",
        "\"Wise choice.\" It sounds like it means the opposite.",
        "The coin disappears into its cloak without a sound. \"Safe travels. More or less.\"",
        "\"Good. Now go use it before I'm selling your gear to the next one.\"",
        "It nods once, slow. Transaction complete.",
        "\"Don't waste it.\""
    };

    // Shown when player opens shop and closes without buying
    /// <summary>Lines shown when the player browses the shop but leaves without buying.</summary>
    public static readonly string[] NoBuy =
    {
        "\"Another time, then.\"",
        "\"Your loss.\" A pause. \"Probably yours, anyway.\"",
        "It watches you leave with no visible disappointment. Somehow that's worse.",
        "\"Still here if you change your mind. I'm always here.\"",
        "\"Die less next time. Might improve your budget.\"",
        "\"Mm.\" Nothing more.",
        "The figure returns to whatever it was doing before you arrived."
    };

    // Shown after the player sells an item
    /// <summary>Lines shown after the player successfully sells an item.</summary>
    public static readonly string[] AfterSale =
    {
        "\"I'll put it to better use,\" the merchant says, barely glancing up.",
        "The merchant tucks the item away without ceremony.",
        "\"A fair trade,\" the merchant mutters, already looking elsewhere.",
        "The merchant examines it briefly, nods, and pockets it.",
        "\"I've been looking for one of these.\" The merchant doesn't elaborate.",
        "The merchant weighs it in their hand. Satisfied.",
        "\"Done.\" The merchant says nothing more.",
        "The coin clinks into your pouch. The item disappears under the counter.",
    };

    // Shown when player tries to sell but has nothing to sell
    /// <summary>Lines shown when the player opens the sell menu with nothing to sell.</summary>
    public static readonly string[] NoSell =
    {
        "The merchant watches you leave. Nothing to say.",
        "\"Nothing worth selling? Come back when you do.\"",
        "The merchant shrugs and turns back to their wares.",
        "\"Empty pockets won't fill mine.\"",
        "The merchant sighs quietly.",
        "\"If you find something worth trading, you know where I am.\"",
    };

    /// <summary>Returns a random line from <see cref="AfterSale"/>.</summary>
    public static string GetAfterSale() => AfterSale[_rng.Next(AfterSale.Length)];
    /// <summary>Returns a random line from <see cref="NoSell"/>.</summary>
    public static string GetNoSell() => NoSell[_rng.Next(NoSell.Length)];

    // Shown when the player sells a Legendary item
    /// <summary>Lines shown when the player sells a Legendary-tier item.</summary>
    public static readonly string[] LegendarySold =
    {
        "\"Where did you get this? ...I won't ask. Here.\"",
        "The merchant's eyes widen — just for a moment. \"I'll pay well for this. Don't tell anyone.\"",
        "\"This is... exceptional. I won't question the blood on it. Take your coin.\""
    };

    // Shown when player tries to buy but cannot afford it
    /// <summary>Lines shown when the player tries to buy but does not have enough gold.</summary>
    public static readonly string[] CantAfford =
    {
        "\"...Come back when you have the coin.\"",
        "\"Not enough. The dungeon doesn't do credit.\"",
        "\"That's sweet. But no.\""
    };

    // Shown when player's inventory is full
    /// <summary>Lines shown when the player's inventory is full.</summary>
    public static readonly string[] InventoryFull =
    {
        "\"You couldn't carry another thing. Clear some space.\"",
        "\"You're already overloaded. Come back when you've made room.\"",
        "\"No room. Drop something first.\""
    };

    // Floor-aware greetings

    /// <summary>Merchant greetings for floors 1-2 (newcomer energy).</summary>
    public static readonly string[] GreetingsFloor1_2 =
    {
        "\"Welcome, brave fool. What'll it be?\"",
        "\"First few floors. You've got that look — half hope, half panic. I can work with that.\"",
        "\"Fresh blood. Literally, by the looks of it. Need something?\"",
        "\"Early days, friend. Best stock up while you still have the gold for it.\"",
        "\"You look new. Don't worry — everyone who walks in here looks new once.\""
    };

    /// <summary>Merchant greetings for floors 3-4 (mildly impressed).</summary>
    public static readonly string[] GreetingsFloor3_4 =
    {
        "\"You've made it this far. Impressive.\"",
        "\"Most don't reach this depth. You must be doing something right. Or lucky.\"",
        "\"Still standing. Good. I prefer customers who can walk in on their own.\"",
        "\"Mid-dungeon. Things get stranger from here. Best prepare now.\"",
        "\"Floor three or four. You've earned a little respect. What do you need?\""
    };

    /// <summary>Merchant greetings for floors 5-6 (rare clientele).</summary>
    public static readonly string[] GreetingsFloor5_6 =
    {
        "\"Not many customers reach this depth.\"",
        "\"You're in rare company. Most of my clients stopped at floor two. Permanently.\"",
        "\"The dungeon gets less forgiving from here. Let me help where I can.\"",
        "\"You have the look of someone who's seen things. Good. It'll keep you sharp.\"",
        "\"Floors five and six. Most folk only see these in their nightmares.\""
    };

    /// <summary>Merchant greetings for floors 7-8 (dark, ominous).</summary>
    public static readonly string[] GreetingsFloor7_8 =
    {
        "\"Still alive? You must want something badly.\"",
        "\"I didn't expect to see a customer. Not down this far. Not a living one.\"",
        "\"The bottom of the dungeon. Whatever brought you here — I hope it was worth it.\"",
        "\"You've outlived everyone who came before you this run. That's either skill or stubbornness.\"",
        "\"Dark place for commerce. Then again, you're still here. So am I. We manage.\""
    };

    /// <summary>
    /// Returns a random floor-appropriate merchant greeting based on the current floor.
    /// </summary>
    /// <param name="floor">The current dungeon floor (1-8).</param>
    public static string GetFloorGreeting(int floor)
    {
        var pool = floor switch
        {
            <= 2 => GreetingsFloor1_2,
            <= 4 => GreetingsFloor3_4,
            <= 6 => GreetingsFloor5_6,
            _    => GreetingsFloor7_8
        };
        return pool[_rng.Next(pool.Length)];
    }

    /// <summary>Returns a random line from <see cref="LegendarySold"/>.</summary>
    public static string GetLegendarySold() => LegendarySold[_rng.Next(LegendarySold.Length)];
    /// <summary>Returns a random line from <see cref="CantAfford"/>.</summary>
    public static string GetCantAfford() => CantAfford[_rng.Next(CantAfford.Length)];
    /// <summary>Returns a random line from <see cref="InventoryFull"/>.</summary>
    public static string GetInventoryFull() => InventoryFull[_rng.Next(InventoryFull.Length)];
}
