namespace Dungnz.Systems;

/// <summary>
/// Flavor text pools for merchant encounters.
/// </summary>
public static class MerchantNarration
{
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
}
