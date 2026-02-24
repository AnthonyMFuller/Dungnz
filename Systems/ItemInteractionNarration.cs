namespace Dungnz.Systems;

using Dungnz.Models;

/// <summary>
/// Flavor text pools for item interactions: picking up, equipping, and consuming items.
/// Tone: grim, terse, pragmatic.
/// </summary>
public static class ItemInteractionNarration
{
    /// <summary>Shown when the player picks up a weapon.</summary>
    public static readonly string[] PickUpWeapon =
    {
        "The blade is cold and ready. You take it.",
        "You test the grip. It'll do.",
        "Heavier than it looks. You take it anyway.",
        "A weapon is a weapon. You pocket it.",
        "You wrap your fingers around the hilt. Better than nothing.",
        "It's seen use. So have you.",
        "You take it without ceremony."
    };

    /// <summary>Shown when the player picks up armor.</summary>
    public static readonly string[] PickUpArmor =
    {
        "Worn leather, still serviceable. You strap it on over your shoulder.",
        "It's battered but it'll hold. You take it.",
        "You check for cracks, find a few, take it anyway.",
        "Protection is protection. Into the pack it goes.",
        "You pick it up. Something about it feels like it belongs with you.",
        "Heavy, practical, yours. You carry it.",
        "Someone left this here. Now it's yours."
    };

    /// <summary>Shown when the player picks up a consumable.</summary>
    public static readonly string[] PickUpConsumable =
    {
        "A potion — murky and warm. You pocket it.",
        "You uncork it, sniff it, decide that's a mistake. Into your pack.",
        "It sloshes. That's usually a good sign.",
        "You've seen worse in these halls. You take it.",
        "Could be useful. Could be poison. Down here that's almost the same thing.",
        "You add it to the pack with the careful hands of someone who's run out before.",
        "Small and fragile. You're careful with it."
    };

    /// <summary>Shown when the player picks up an accessory or other item type.</summary>
    public static readonly string[] PickUpOther =
    {
        "You turn it over in your hand, then take it.",
        "Odd thing to find here. You keep it.",
        "You're not sure what it does yet. That's fine.",
        "It catches the light wrong. You take it anyway.",
        "Into the pack. Sort it out later.",
        "You tuck it away. Everything has its use.",
        "Curious. You pocket it without thinking too hard about it."
    };

    /// <summary>Shown when the player equips a weapon.</summary>
    public static readonly string[] EquipWeapon =
    {
        "You feel the weight shift in your grip. Better.",
        "The balance is off but manageable. You'll get used to it.",
        "You roll your wrist, test the swing. It'll do.",
        "Gripped and ready. You feel marginally less vulnerable.",
        "Heavier than your last. You adjust.",
        "You slot it into your hand. Familiar and cold.",
        "The edge looks honest. You trust it."
    };

    /// <summary>Shown when the player equips armor.</summary>
    public static readonly string[] EquipArmor =
    {
        "The buckles pull tight. It fits well enough.",
        "You strap it on, shift your shoulders, accept the weight.",
        "Not comfortable. But you're not down here to be comfortable.",
        "You check the fastenings, find them solid.",
        "It smells like the last person who wore it. You push that thought down.",
        "Snug, worn, protective. Three things you can work with.",
        "It settles on you like it was always going to be yours."
    };

    /// <summary>Shown when the player equips an accessory.</summary>
    public static readonly string[] EquipAccessory =
    {
        "You fasten it and feel something shift — subtly.",
        "Strange to wear. Stranger not to.",
        "It fits. That's enough.",
        "You put it on and wait. Something changes, or doesn't. Hard to say.",
        "Odd thing to wear in a dungeon. You wear it anyway.",
        "Light and unassuming. You almost forget it's there.",
        "You snap it into place. Let's see what it does."
    };

    /// <summary>
    /// Shown when the player uses a healing consumable.
    /// Some lines contain <c>{0}</c> as a placeholder for the actual HP restored.
    /// </summary>
    public static readonly string[] UseHealingConsumable =
    {
        "The warmth spreads fast. You remember what full lungs feel like.",
        "It burns going down, then it doesn't. {0} HP back.",
        "Bitter, medicinal, necessary. You feel the wounds close a little.",
        "You drink without tasting it. {0} HP — you'll take it.",
        "The pain doesn't leave. It just… steps back.",
        "You steady your breath. The potion does the rest.",
        "Rough stuff, but effective. You're still here."
    };

    /// <summary>Shown when the player uses a mana-restoring consumable.</summary>
    public static readonly string[] UseManaConsumable =
    {
        "Something cold runs through you. The fog behind your eyes lifts.",
        "Sharp and electric. Your focus snaps back.",
        "You feel the reserves fill. Not comfortable. Useful.",
        "Like breathing cold air after a long hold. Clarity returns.",
        "The buzz fades quickly. What stays is better.",
        "Your thoughts clear. The cost was worth it.",
        "You exhale slow. The mana follows."
    };

    /// <summary>Shown when the player uses a non-healing, non-mana consumable (e.g. a stat boost).</summary>
    public static readonly string[] UseOtherConsumable =
    {
        "You feel the effect settle into your body.",
        "It's done. Something is different — you're not sure what yet.",
        "You consume it and wait. The dungeon doesn't.",
        "Not unpleasant. You note the change and move on.",
        "It works. That's what matters.",
        "You use it without hesitation. Hesitation is expensive down here.",
        "Something shifts. You accept it and press on."
    };

    /// <summary>Returns a random pickup flavor line appropriate for the given item's type.</summary>
    public static string PickUp(Item item)
    {
        var pool = item.Type switch
        {
            ItemType.Weapon     => PickUpWeapon,
            ItemType.Armor      => PickUpArmor,
            ItemType.Consumable => PickUpConsumable,
            _                   => PickUpOther
        };
        return pool[Random.Shared.Next(pool.Length)];
    }

    /// <summary>Returns a random equip flavor line appropriate for the given item's type.</summary>
    public static string Equip(Item item)
    {
        var pool = item.Type switch
        {
            ItemType.Weapon    => EquipWeapon,
            ItemType.Armor     => EquipArmor,
            ItemType.Accessory => EquipAccessory,
            _                  => EquipWeapon
        };
        return pool[Random.Shared.Next(pool.Length)];
    }

    /// <summary>
    /// Returns a random consumable-use flavor line.
    /// Healing lines may embed <paramref name="healAmount"/> where the pool entry contains <c>{0}</c>.
    /// Pass <c>0</c> for <paramref name="healAmount"/> when the item is not a healing consumable.
    /// </summary>
    public static string UseConsumable(Item item, int healAmount)
    {
        if (item.HealAmount > 0)
        {
            var line = UseHealingConsumable[Random.Shared.Next(UseHealingConsumable.Length)];
            return line.Contains("{0}") ? string.Format(line, healAmount) : line;
        }
        if (item.ManaRestore > 0)
            return UseManaConsumable[Random.Shared.Next(UseManaConsumable.Length)];
        return UseOtherConsumable[Random.Shared.Next(UseOtherConsumable.Length)];
    }
}
