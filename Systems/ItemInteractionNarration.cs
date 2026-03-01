namespace Dungnz.Systems;

using Dungnz.Models;

/// <summary>
/// Flavor text pools for item interactions: picking up, equipping, and consuming items.
/// Tone: grim, terse, pragmatic.
/// </summary>
public static class ItemInteractionNarration
{
    // -----------------------------------------------------------------------
    // Pickup -- generic type fallbacks (Common tier or type-only lookup)
    // -----------------------------------------------------------------------

    /// <summary>Shown when the player picks up a weapon (Common tier fallback).</summary>
    public static readonly string[] PickUpWeapon =
    {
        "The blade is cold and ready. You take it.",
        "You test the grip. It'll do.",
        "Heavier than it looks. You take it anyway.",
        "A weapon is a weapon. You pocket it.",
        "You wrap your fingers around the hilt. Better than nothing.",
        "It's seen use. So have you.",
        "You take it without ceremony.",
    };

    /// <summary>Shown when the player picks up armor (Common tier fallback).</summary>
    public static readonly string[] PickUpArmor =
    {
        "Worn leather, still serviceable. You strap it on over your shoulder.",
        "It's battered but it'll hold. You take it.",
        "You check for cracks, find a few, take it anyway.",
        "Protection is protection. Into the pack it goes.",
        "You pick it up. Something about it feels like it belongs with you.",
        "Heavy, practical, yours. You carry it.",
        "Someone left this here. Now it's yours.",
    };

    /// <summary>Shown when the player picks up a consumable.</summary>
    public static readonly string[] PickUpConsumable =
    {
        "A potion -- murky and warm. You pocket it.",
        "You uncork it, sniff it, decide that's a mistake. Into your pack.",
        "It sloshes. That's usually a good sign.",
        "You've seen worse in these halls. You take it.",
        "Could be useful. Could be poison. Down here that's almost the same thing.",
        "You add it to the pack with the careful hands of someone who's run out before.",
        "Small and fragile. You're careful with it.",
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
        "Curious. You pocket it without thinking too hard about it.",
    };

    // -----------------------------------------------------------------------
    // Pickup -- by item tier (Phase 8-E2)
    // -----------------------------------------------------------------------

    /// <summary>Common-tier pickup: matter-of-fact, no ceremony.</summary>
    public static readonly string[] PickUpCommon =
    {
        "You pocket it. Nothing special.",
        "It'll do the job. You take it.",
        "Common gear. You've held worse.",
        "Into the pack without a second thought.",
        "Functional. You keep it.",
    };

    /// <summary>Uncommon-tier pickup: slight appreciation.</summary>
    public static readonly string[] PickUpUncommon =
    {
        "Not bad. You turn it over and nod.",
        "Better than average. You keep it.",
        "This one's worth carrying. Into the pack.",
        "You feel the quality difference immediately. Good find.",
        "Decent craftsmanship. You don't leave this behind.",
    };

    /// <summary>Rare-tier pickup: notable, warrants a pause.</summary>
    public static readonly string[] PickUpRare =
    {
        "You heft it. Substantial. This is real gear.",
        "Better than anything you've found today. You take it carefully.",
        "The balance is exceptional. You notice it immediately.",
        "This was made by someone who knew what they were doing.",
        "You feel the weight of it properly. Worth every pound.",
    };

    /// <summary>Epic-tier pickup: impressed, aware this is significant.</summary>
    public static readonly string[] PickUpEpic =
    {
        "This is the real thing. You feel stronger just holding it.",
        "Your hand closes around it and something clicks into place.",
        "Epic gear. Down here. You almost don't believe your luck.",
        "You feel it before you see it clearly. Power, concentrated.",
        "The craftsmanship is beyond what you've seen. You take it reverently.",
    };

    /// <summary>Legendary-tier pickup: reverent, almost trembling.</summary>
    public static readonly string[] PickUpLegendary =
    {
        "Your hands tremble slightly. Something this powerful shouldn't exist.",
        "You hold it and the dungeon feels quieter. It knows.",
        "Legendary. You say the word quietly, to no one.",
        "This shouldn't be lying on the floor. You take it anyway.",
        "The air changes when you pick it up. You are different now.",
    };

    // -----------------------------------------------------------------------
    // Equip -- generic type fallbacks
    // -----------------------------------------------------------------------

    /// <summary>Shown when the player equips a weapon.</summary>
    public static readonly string[] EquipWeapon =
    {
        "You feel the weight shift in your grip. Better.",
        "The balance is off but manageable. You'll get used to it.",
        "You roll your wrist, test the swing. It'll do.",
        "Gripped and ready. You feel marginally less vulnerable.",
        "Heavier than your last. You adjust.",
        "You slot it into your hand. Familiar and cold.",
        "The edge looks honest. You trust it.",
    };

    /// <summary>Shown when the player equips armor (generic slot fallback).</summary>
    public static readonly string[] EquipArmor =
    {
        "The buckles pull tight. It fits well enough.",
        "You strap it on, shift your shoulders, accept the weight.",
        "Not comfortable. But you're not down here to be comfortable.",
        "You check the fastenings, find them solid.",
        "It smells like the last person who wore it. You push that thought down.",
        "Snug, worn, protective. Three things you can work with.",
        "It settles on you like it was always going to be yours.",
    };

    /// <summary>Shown when the player equips an accessory.</summary>
    public static readonly string[] EquipAccessory =
    {
        "You fasten it and feel something shift -- subtly.",
        "Strange to wear. Stranger not to.",
        "It fits. That's enough.",
        "You put it on and wait. Something changes, or doesn't. Hard to say.",
        "Odd thing to wear in a dungeon. You wear it anyway.",
        "Light and unassuming. You almost forget it's there.",
        "You snap it into place. Let's see what it does.",
    };

    // -----------------------------------------------------------------------
    // Equip -- by armor slot (Phase 8-E2)
    // -----------------------------------------------------------------------

    /// <summary>Equipping a helm -- world narrows, peripheral vision cut off.</summary>
    public static readonly string[] EquipHead =
    {
        "You pull on the helm. The world narrows to a slit of iron.",
        "It settles heavy on your skull. You think differently in it.",
        "The chin-strap bites. You tighten it anyway.",
    };

    /// <summary>Equipping chest armor -- breathing adjusts to the weight.</summary>
    public static readonly string[] EquipChest =
    {
        "You buckle the armor. Your breathing adjusts to the weight.",
        "The chest plate settles. You roll your shoulders and accept it.",
        "Heavy across the ribs, but that's the point. Buckled tight.",
    };

    /// <summary>Equipping hand armor -- gauntlets flex around the fist.</summary>
    public static readonly string[] EquipHands =
    {
        "The gauntlets close around your fists. You flex them once.",
        "Snug at the knuckles. Your grip is different -- better.",
        "You close your hands inside the gloves and feel the reinforcement.",
    };

    /// <summary>Equipping leg armor -- movement and weight sensation.</summary>
    public static readonly string[] EquipLegs =
    {
        "The greaves strap on. Your stride shortens -- and strengthens.",
        "Heavier at the thighs. You adjust your balance and move.",
        "The leg plates shift as you test your footing. Solid.",
    };

    /// <summary>Equipping boot armor -- footing and groundedness.</summary>
    public static readonly string[] EquipFeet =
    {
        "The boots settle. The ground feels different under them.",
        "Good footing. You test a step and nod.",
        "Heavy at the ankle, sure at the sole. You'll take it.",
    };

    /// <summary>Equipping shoulder armor -- weight and readiness.</summary>
    public static readonly string[] EquipShoulders =
    {
        "The pauldrons lock on. You carry the weight forward.",
        "Something about bearing armored shoulders changes your posture.",
        "They're heavier than they look. You stand straighter anyway.",
    };

    /// <summary>Equipping a cloak or back armor -- warmth and coverage.</summary>
    public static readonly string[] EquipBack =
    {
        "The cloak settles over your shoulders. The dark takes you in.",
        "It's warmer than you expected. You pull it close.",
        "Coverage at your back. The dungeon can try.",
    };

    /// <summary>Equipping an off-hand item -- balance and shield-wall feeling.</summary>
    public static readonly string[] EquipOffHand =
    {
        "The shield locks onto your arm. Balance shifts. Better.",
        "You feel the difference in your stance immediately -- grounded.",
        "Off-hand secure. You're harder to kill now.",
    };

    // -----------------------------------------------------------------------
    // Consume -- generic
    // -----------------------------------------------------------------------

    /// <summary>
    /// Shown when the player uses a healing consumable.
    /// Some lines contain {0} as a placeholder for the actual HP restored.
    /// </summary>
    public static readonly string[] UseHealingConsumable =
    {
        "The warmth spreads fast. You remember what full lungs feel like.",
        "It burns going down, then it doesn't. {0} HP back.",
        "Bitter, medicinal, necessary. You feel the wounds close a little.",
        "You drink without tasting it. {0} HP -- you'll take it.",
        "The pain doesn't leave. It just steps back.",
        "You steady your breath. The potion does the rest.",
        "Rough stuff, but effective. You're still here.",
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
        "You exhale slow. The mana follows.",
    };

    /// <summary>Shown when the player uses a non-healing, non-mana consumable.</summary>
    public static readonly string[] UseOtherConsumable =
    {
        "You feel the effect settle into your body.",
        "It's done. Something is different -- you're not sure what yet.",
        "You consume it and wait. The dungeon doesn't.",
        "Not unpleasant. You note the change and move on.",
        "It works. That's what matters.",
        "You use it without hesitation. Hesitation is expensive down here.",
        "Something shifts. You accept it and press on.",
    };

    // -----------------------------------------------------------------------
    // Consume -- specific items (Phase 8-E2)
    // -----------------------------------------------------------------------

    /// <summary>Elixir category -- more mystical than a plain potion.</summary>
    public static readonly string[] UseElixir =
    {
        "The elixir moves through you like light through smoke.",
        "Warmer than a potion. More certain. The effect is immediate.",
        "You drink it slowly. The magic doesn't rush -- it settles.",
        "Something old and precise runs its course through your veins.",
    };

    /// <summary>Panacea -- a cure-all; relief, clarity, wonder.</summary>
    public static readonly string[] UsePanacea =
    {
        "Everything that was wrong stops being wrong. Just like that.",
        "Panacea. The name isn't an exaggeration.",
        "You feel every ailment leave in order. Methodical. Complete.",
        "Whole again. The dungeon had a lot to answer for.",
    };

    /// <summary>Berserk Tonic -- violent, immediate, barely controlled.</summary>
    public static readonly string[] UseBerserkTonic =
    {
        "The tonic hits and the world goes red at the edges.",
        "Your hands shake. Not from fear. From restraint.",
        "Something primal surfaces. You point it at the enemy.",
        "The berserk surge floods in. You'll deal with the aftermath later.",
    };

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    /// <summary>Returns a random pickup flavor line appropriate for the item's tier and type.</summary>
    public static string PickUp(Item item)
    {
        var tierPool = item.Tier switch
        {
            ItemTier.Uncommon  => PickUpUncommon,
            ItemTier.Rare      => PickUpRare,
            ItemTier.Epic      => PickUpEpic,
            ItemTier.Legendary => PickUpLegendary,
            _                  => null,
        };
        if (tierPool != null)
            return tierPool[Random.Shared.Next(tierPool.Length)];

        var pool = item.Type switch
        {
            ItemType.Weapon          => PickUpWeapon,
            ItemType.Armor           => PickUpArmor,
            ItemType.Consumable      => PickUpConsumable,
            ItemType.CraftingMaterial => PickUpOther,
            _                        => PickUpOther,
        };
        return pool[Random.Shared.Next(pool.Length)];
    }

    /// <summary>Returns a random equip flavor line appropriate for the item's slot or type.</summary>
    public static string Equip(Item item)
    {
        if (item.Type == ItemType.Armor)
        {
            var slotPool = item.Slot switch
            {
                ArmorSlot.Head      => EquipHead,
                ArmorSlot.Chest     => EquipChest,
                ArmorSlot.Hands     => EquipHands,
                ArmorSlot.Legs      => EquipLegs,
                ArmorSlot.Feet      => EquipFeet,
                ArmorSlot.Shoulders => EquipShoulders,
                ArmorSlot.Back      => EquipBack,
                ArmorSlot.OffHand   => EquipOffHand,
                _                   => EquipArmor,
            };
            return slotPool[Random.Shared.Next(slotPool.Length)];
        }

        var pool = item.Type switch
        {
            ItemType.Weapon    => EquipWeapon,
            ItemType.Accessory => EquipAccessory,
            _                  => EquipWeapon,
        };
        return pool[Random.Shared.Next(pool.Length)];
    }

    /// <summary>
    /// Returns a random consumable-use flavor line.
    /// Checks specific item IDs first, then category, then generic type.
    /// Healing lines may embed healAmount where the pool contains {0}.
    /// Pass 0 for healAmount when the item is not a healing consumable.
    /// </summary>
    public static string UseConsumable(Item item, int healAmount)
    {
        if (item.Id == "panacea")
            return UsePanacea[Random.Shared.Next(UsePanacea.Length)];

        if (item.Id == "berserk-tonic")
            return UseBerserkTonic[Random.Shared.Next(UseBerserkTonic.Length)];

        if (item.Id.Contains("elixir"))
            return UseElixir[Random.Shared.Next(UseElixir.Length)];

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
