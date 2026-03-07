namespace Dungnz.Engine.Commands;

using Dungnz.Models;
using Dungnz.Systems;

internal sealed class UseCommandHandler : ICommandHandler
{
    public void Handle(string argument, CommandContext context)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            context.TurnConsumed = false;
            var usable = context.Player.Inventory.Where(i => i.Type == ItemType.Consumable).ToList();
            if (usable.Count == 0)
            {
                context.Display.ShowError("You have no usable items in your inventory.");
                context.Display.ShowRoom(context.CurrentRoom);
                return;
            }
            var selected = context.Display.ShowUseMenuAndSelect(usable.AsReadOnly());
            if (selected == null) { context.Display.ShowRoom(context.CurrentRoom); return; }
            argument = selected.Name;
        }

        // Special: USE SHRINE
        if (argument.Equals("shrine", StringComparison.OrdinalIgnoreCase))
        {
            context.HandleShrine();
            return;
        }

        // Special: USE ARMORY
        if (argument.Equals("armory", StringComparison.OrdinalIgnoreCase))
        {
            context.HandleContestedArmory();
            return;
        }

        var itemNameLower = argument.ToLowerInvariant();
        var item = context.Player.Inventory.FirstOrDefault(i => i.Name.ToLowerInvariant().Contains(itemNameLower));

        if (item == null)
        {
            // Pass 2: fuzzy Levenshtein distance match
            int tolerance = Math.Max(3, itemNameLower.Length / 2);
            var candidates = context.Player.Inventory
                .Select(i => (Item: i, Distance: EquipmentManager.LevenshteinDistance(itemNameLower, i.Name.ToLowerInvariant())))
                .Where(x => x.Distance <= tolerance)
                .ToList();

            if (candidates.Count == 0)
            {
                context.TurnConsumed = false;
                context.Display.ShowError($"You don't have '{argument}'.");
                context.Display.ShowRoom(context.CurrentRoom);
                return;
            }

            int bestDistance = candidates.Min(x => x.Distance);
            var bestCandidates = candidates.Where(x => x.Distance == bestDistance).ToList();

            if (bestCandidates.Count > 1)
            {
                context.TurnConsumed = false;
                var names = string.Join(", ", bestCandidates.Select(x => x.Item.Name));
                context.Display.ShowError($"Did you mean one of: {names}? Please be more specific.");
                context.Display.ShowRoom(context.CurrentRoom);
                return;
            }

            item = bestCandidates[0].Item;
            context.Display.ShowMessage($"(Did you mean \"{item.Name}\"?)");
        }

        switch (item.Type)
        {
            case ItemType.Consumable:
                if (!string.IsNullOrEmpty(item.Description))
                    context.Display.ShowMessage(item.Description);
                if (item.HealAmount > 0)
                {
                    var healAmt = Math.Max(1, (int)(item.HealAmount * context.Difficulty.HealingMultiplier));
                    var oldHP = context.Player.HP;
                    context.Player.Heal(healAmt);
                    var healedAmount = context.Player.HP - oldHP;
                    context.Player.Inventory.Remove(item);
                    context.Display.ShowMessage($"You use {item.Name} and restore {healedAmount} HP. Current HP: {context.Player.HP}/{context.Player.MaxHP}");
                    context.Display.ShowMessage(ItemInteractionNarration.UseConsumable(item, healedAmount));
                }
                else if (item.ManaRestore > 0)
                {
                    var oldMana = context.Player.Mana;
                    context.Player.RestoreMana(item.ManaRestore);
                    var restoredMana = context.Player.Mana - oldMana;
                    context.Player.Inventory.Remove(item);
                    context.Display.ShowMessage($"You use {item.Name} and restore {restoredMana} mana. Mana: {context.Player.Mana}/{context.Player.MaxMana}");
                    context.Display.ShowMessage(ItemInteractionNarration.UseConsumable(item, 0));
                }
                else if (item.AttackBonus > 0)
                {
                    context.Player.ModifyAttack(item.AttackBonus);
                    context.Player.Inventory.Remove(item);
                    context.Display.ShowMessage($"You use {item.Name}. Attack permanently +{item.AttackBonus}. Attack: {context.Player.Attack}");
                    context.Display.ShowMessage(ItemInteractionNarration.UseConsumable(item, 0));
                }
                else if (item.DefenseBonus > 0)
                {
                    context.Player.ModifyDefense(item.DefenseBonus);
                    context.Player.Inventory.Remove(item);
                    context.Display.ShowMessage($"You use {item.Name}. Defense permanently +{item.DefenseBonus}. Defense: {context.Player.Defense}");
                    context.Display.ShowMessage(ItemInteractionNarration.UseConsumable(item, 0));
                }
                else if (item.PassiveEffectId == "bone_flute")
                {
                    context.Player.ActiveMinions.Add(new Models.Minion { Name = "Skeletal Ally", HP = 60, MaxHP = 60, ATK = 15, AttackFlavorText = "The Skeletal Ally rattles forward and strikes!" });
                    context.Player.Inventory.Remove(item);
                    context.Display.ShowMessage("The flute's hollow note summons a Skeletal Ally to fight alongside you!");
                    context.Display.ShowMessage(ItemInteractionNarration.UseConsumable(item, 0));
                }
                else if (item.PassiveEffectId == "dragonheart_elixir")
                {
                    context.Player.FortifyMaxHP(100);
                    context.Player.Inventory.Remove(item);
                    context.Display.ShowMessage($"Dragonheart warmth spreads through you. MaxHP +100! ({context.Player.MaxHP} MaxHP)");
                    context.Display.ShowMessage(ItemInteractionNarration.UseConsumable(item, 0));
                }
                else if (item.PassiveEffectId == "cure_all")
                {
                    context.Player.ActiveEffects.RemoveAll(e => e.IsDebuff);
                    context.Player.Heal(context.Player.MaxHP);
                    context.Player.Inventory.Remove(item);
                    context.Display.ShowMessage($"The Panacea purges all ailments and restores you to full health. HP: {context.Player.HP}/{context.Player.MaxHP}");
                    context.Display.ShowMessage(ItemInteractionNarration.UseConsumable(item, 0));
                }
                else if (item.PassiveEffectId == "berserk_buff")
                {
                    int atkGain = Math.Max(1, context.Player.Attack / 2);
                    int defLoss = Math.Max(1, context.Player.Defense * 3 / 10);
                    context.Player.ModifyAttack(atkGain);
                    context.Player.TempAttackBonus += atkGain;
                    context.Player.ModifyDefense(-defLoss);
                    context.Player.TempDefenseBonus -= defLoss;
                    context.Player.Inventory.Remove(item);
                    context.Display.ShowMessage($"Rage floods your veins. ATK +{atkGain}, DEF -{defLoss} until next floor. ATK: {context.Player.Attack}, DEF: {context.Player.Defense}");
                    context.Display.ShowMessage(ItemInteractionNarration.UseConsumable(item, 0));
                }
                else if (item.PassiveEffectId == "stone_skin_buff")
                {
                    int defGain = Math.Max(1, context.Player.Defense * 2 / 5);
                    context.Player.ModifyDefense(defGain);
                    context.Player.TempDefenseBonus += defGain;
                    context.Player.Inventory.Remove(item);
                    context.Display.ShowMessage($"Your skin hardens to granite. DEF +{defGain} until next floor. DEF: {context.Player.Defense}");
                    context.Display.ShowMessage(ItemInteractionNarration.UseConsumable(item, 0));
                }
                else if (item.PassiveEffectId == "swiftness_buff")
                {
                    int atkGain = Math.Max(1, context.Player.Attack / 4);
                    context.Player.ModifyAttack(atkGain);
                    context.Player.TempAttackBonus += atkGain;
                    context.Player.Inventory.Remove(item);
                    context.Display.ShowMessage($"The world slows; you do not. ATK +{atkGain} until next floor. ATK: {context.Player.Attack}");
                    context.Display.ShowMessage(ItemInteractionNarration.UseConsumable(item, 0));
                }
                else
                {
                    context.TurnConsumed = false;
                    context.Display.ShowMessage("Nothing happened.");
                    context.Display.ShowError($"You can't use {item.Name} right now.");
                }
                break;

            case ItemType.Weapon:
            case ItemType.Armor:
            case ItemType.Accessory:
                context.TurnConsumed = false;
                context.Display.ShowError($"Use 'EQUIP {item.Name}' to equip this item.");
                break;

            case ItemType.CraftingMaterial:
                context.TurnConsumed = false;
                context.Display.ShowError($"{item.Name} is a crafting material and cannot be used directly. Use it at a crafting station.");
                break;

            default:
                context.TurnConsumed = false;
                context.Display.ShowError($"You can't use {item.Name}.");
                break;
        }
        
        context.Display.ShowRoom(context.CurrentRoom);
    }
}
