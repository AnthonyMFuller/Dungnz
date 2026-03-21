using CommunityToolkit.Mvvm.ComponentModel;
using Dungnz.Models;
using Dungnz.Systems;
using System.Text;

namespace Dungnz.Display.Avalonia.ViewModels;

/// <summary>
/// View model for the stats panel showing player HP, MP, level, gear, etc.
/// </summary>
public partial class StatsPanelViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statsText = "Stats will appear here";

    /// <summary>
    /// Updates the stats panel with player stats in exploration mode.
    /// </summary>
    public void Update(Player player, IReadOnlyList<(string name, int turnsRemaining)> cooldowns)
    {
        StatsText = BuildPlayerStatsText(player, cooldowns);
    }

    /// <summary>
    /// Updates the stats panel with player stats in combat mode.
    /// Enemy stats are displayed separately in the Gear panel.
    /// </summary>
    public void UpdateCombat(Player player, IReadOnlyList<(string name, int turnsRemaining)> cooldowns)
    {
        StatsText = BuildPlayerStatsText(player, cooldowns);
    }

    private static string BuildPlayerStatsText(Player player, IReadOnlyList<(string name, int turnsRemaining)> cooldowns)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{ColorCodes.BrightWhite}{player.Name}{ColorCodes.Reset}  {ColorCodes.Yellow}Lv {player.Level}{ColorCodes.Reset}  {player.Class}");
        sb.AppendLine();

        var hpColor = ColorCodes.HealthColor(player.HP, player.MaxHP);
        var hpBar = BuildPlainHpBar(player.HP, player.MaxHP);
        sb.AppendLine($"HP {hpColor}{hpBar} {player.HP}/{player.MaxHP}{ColorCodes.Reset}");

        if (player.MaxMana > 0)
        {
            var mpColor = ColorCodes.ManaColor(player.Mana, player.MaxMana);
            var mpBar = BuildPlainMpBar(player.Mana, player.MaxMana);
            sb.AppendLine($"MP {mpColor}{mpBar} {player.Mana}/{player.MaxMana}{ColorCodes.Reset}");
        }

        if (cooldowns.Count > 0)
        {
            var cdParts = cooldowns.Select(c =>
                c.turnsRemaining == 0
                    ? $"{c.name}:✅"
                    : $"{c.name}:{c.turnsRemaining}t");
            sb.AppendLine($"CD: {string.Join("  ", cdParts)}");
        }

        sb.AppendLine();
        sb.AppendLine($"ATK {player.Attack}   DEF {player.Defense}");
        sb.AppendLine($"Gold {player.Gold}g");
        var xpToNext = 100 * player.Level;
        sb.AppendLine($"XP {player.XP}/{xpToNext}");

        if (player.Class == PlayerClass.Rogue && player.ComboPoints > 0)
        {
            var dots = new string('●', player.ComboPoints) + new string('○', 5 - player.ComboPoints);
            sb.AppendLine($"❖ Combo {dots}");
        }

        if (player.Momentum is { } momentum)
        {
            var label = player.Class switch
            {
                PlayerClass.Warrior => "Fury",
                PlayerClass.Mage    => "Charge",
                PlayerClass.Paladin => "Devotion",
                PlayerClass.Ranger  => "Focus",
                _                   => "Momentum"
            };
            var dots = new string('●', momentum.Current) + new string('○', momentum.Maximum - momentum.Current);
            var chargedSuffix = momentum.IsCharged ? $" {ColorCodes.Yellow}[CHARGED]{ColorCodes.Reset}" : string.Empty;
            sb.AppendLine($"❖ {label} {dots}{chargedSuffix}");
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

    private static string BuildPlainMpBar(int current, int max, int width = 10)
    {
        if (max <= 0) return string.Empty;
        current = Math.Clamp(current, 0, max);
        int filled = (int)Math.Round((double)current / max * width);
        return new string('█', filled) + new string('░', width - filled);
    }
}
