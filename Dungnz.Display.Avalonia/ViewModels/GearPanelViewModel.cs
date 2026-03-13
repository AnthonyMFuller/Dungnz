using CommunityToolkit.Mvvm.ComponentModel;
using Dungnz.Models;
using Dungnz.Systems;
using System.Text;

namespace Dungnz.Display.Avalonia.ViewModels;

/// <summary>
/// View model for the gear/equipment panel.
/// </summary>
public partial class GearPanelViewModel : ObservableObject
{
    [ObservableProperty]
    private string _gearText = "Gear will appear here";

    /// <summary>
    /// Updates the gear panel with player's equipped items.
    /// </summary>
    public void Update(Player player)
    {
        GearText = BuildGearText(player);
    }

    /// <summary>
    /// Shows enemy stats in the gear panel during combat.
    /// </summary>
    public void ShowEnemyStats(Enemy enemy, IReadOnlyList<ActiveEffect> enemyEffects)
    {
        GearText = BuildEnemyStatsText(enemy, enemyEffects);
    }

    private static string BuildGearText(Player player)
    {
        var sb = new StringBuilder();

        void AddSlot(string slotLabel, Item? item, bool isWeapon = false, bool isAccessory = false)
        {
            if (item == null)
            {
                sb.AppendLine($"{slotLabel}:  (empty)");
                return;
            }
            var statParts = new List<string>();
            if (isWeapon)
            {
                if (item.AttackBonus  != 0) statParts.Add($"+{item.AttackBonus} ATK");
                if (item.DodgeBonus   >  0) statParts.Add($"+{item.DodgeBonus:P0} dodge");
                if (item.MaxManaBonus >  0) statParts.Add($"+{item.MaxManaBonus} mana");
            }
            else if (isAccessory)
            {
                if (item.AttackBonus  != 0) statParts.Add($"+{item.AttackBonus} ATK");
                if (item.DefenseBonus != 0) statParts.Add($"+{item.DefenseBonus} DEF");
                if (item.StatModifier != 0) statParts.Add($"+{item.StatModifier} HP");
                if (item.DodgeBonus   >  0) statParts.Add($"+{item.DodgeBonus:P0} dodge");
            }
            else
            {
                if (item.DefenseBonus != 0) statParts.Add($"+{item.DefenseBonus} DEF");
                if (item.DodgeBonus   >  0) statParts.Add($"+{item.DodgeBonus:P0} dodge");
                if (item.MaxManaBonus >  0) statParts.Add($"+{item.MaxManaBonus} mana");
            }
            var statsStr = statParts.Count > 0 ? "  " + string.Join(", ", statParts) : "";
            sb.AppendLine($"{slotLabel}:  {item.Name}{statsStr}");
        }

        AddSlot("⚔  Weapon",    player.EquippedWeapon,    isWeapon: true);
        AddSlot("💍 Accessory", player.EquippedAccessory, isAccessory: true);
        AddSlot("🪖 Head",      player.EquippedHead);
        AddSlot("🥋 Shoulders", player.EquippedShoulders);
        AddSlot("🦺 Chest",     player.EquippedChest);
        AddSlot("🧤 Hands",     player.EquippedHands);
        AddSlot("👖 Legs",      player.EquippedLegs);
        AddSlot("👟 Feet",      player.EquippedFeet);
        AddSlot("🧥 Back",      player.EquippedBack);
        AddSlot("🔰 Off-Hand",  player.EquippedOffHand);

        var setDesc = SetBonusManager.GetActiveBonusDescription(player);
        if (!string.IsNullOrEmpty(setDesc))
        {
            sb.AppendLine();
            sb.Append($"Set Bonus: {setDesc}");
        }

        return sb.ToString().TrimEnd();
    }

    private static string BuildEnemyStatsText(Enemy enemy, IReadOnlyList<ActiveEffect> enemyEffects)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"🐉 {enemy.Name}");

        if (enemy is Dungnz.Systems.Enemies.DungeonBoss boss)
        {
            var phaseNum = boss.FiredPhases.Count + 1;
            sb.Append($"Phase {phaseNum}");
            if (boss.IsEnraged)
                sb.Append($"  ⚡ ENRAGED");
            sb.AppendLine();
        }

        var hpBar = BuildPlainHpBar(enemy.HP, enemy.MaxHP);
        sb.AppendLine($"HP {hpBar} {enemy.HP}/{enemy.MaxHP}");
        sb.AppendLine($"ATK {enemy.Attack}  DEF {enemy.Defense}");

        var regenParts = new List<string>();
        if (enemy.RegenPerTurn > 0)
            regenParts.Add($"Regen +{enemy.RegenPerTurn}/turn");
        if (enemy.SelfHealAmount > 0 && enemy.SelfHealEveryTurns > 0)
            regenParts.Add($"Heals +{enemy.SelfHealAmount} every {enemy.SelfHealEveryTurns}t");
        if (regenParts.Count > 0)
        {
            sb.Append(string.Join("  ", regenParts));
            sb.AppendLine();
        }

        var badges = new List<string>();
        if (enemy.IsElite)                  badges.Add("⭐ Elite");
        if (enemy.IsUndead)                 badges.Add("💀 Undead");
        if (enemy.IsStunImmune)             badges.Add("🛡 StunImm");
        if (enemy.IsImmuneToEffects)        badges.Add("🔒 EffectImm");
        if (enemy.LifestealPercent > 0)     badges.Add("🩸 Lifesteal");
        if (enemy.AppliesPoisonOnHit)       badges.Add("☠ Poison");
        if (enemy.CounterStrikeChance > 0)  badges.Add("↩ Counter");
        if (enemy.PackCount > 1)            badges.Add($"🐾 Pack×{enemy.PackCount}");
        if (badges.Count > 0)
        {
            sb.Append(string.Join(" ", badges));
            sb.AppendLine();
        }

        if (enemyEffects.Count > 0)
        {
            foreach (var e in enemyEffects)
            {
                sb.Append($"[{EffectIcon(e.Effect)}{e.Effect} {e.RemainingTurns}t] ");
            }
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    private static string BuildPlainHpBar(int current, int max, int width = 10)
    {
        if (max <= 0) return new string('░', width);
        current = Math.Clamp(current, 0, max);
        int filled = (int)Math.Round((double)current / max * width);
        return new string('█', filled) + new string('░', width - filled);
    }

    private static string EffectIcon(StatusEffect effect) => effect switch
    {
        StatusEffect.Poison    => "☠",
        StatusEffect.Bleed     => "🩸",
        StatusEffect.Stun      => "⚡",
        StatusEffect.Regen     => "✨",
        StatusEffect.Fortified => "🛡",
        StatusEffect.Weakened  => "💀",
        StatusEffect.Slow      => ">",
        StatusEffect.BattleCry => "!",
        StatusEffect.Burn      => "*",
        StatusEffect.Freeze    => "~",
        StatusEffect.Silence   => "X",
        StatusEffect.Curse     => "@",
        _                      => "●"
    };
}
