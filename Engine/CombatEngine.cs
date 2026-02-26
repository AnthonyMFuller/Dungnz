namespace Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Display;
using Dungnz.Systems;
using Dungnz.Systems.Enemies;

/// <summary>
/// Full turn-based combat engine that drives fight encounters between the player and
/// an enemy, handling player input, ability usage, status effects, boss mechanics
/// (enrage, telegraphed charge, ambush), loot distribution, and XP/level-up logic.
/// </summary>
public class CombatEngine : ICombatEngine
{
    private readonly IDisplayService _display;
    private readonly IInputReader _input;
    private readonly Random _rng;
    private readonly GameEvents? _events;
    private readonly StatusEffectManager _statusEffects;
    private readonly AbilityManager _abilities;
    private readonly NarrationService _narration;
    private readonly InventoryManager _inventoryManager;
    private readonly List<CombatTurn> _turnLog = new();
    private RunStats _stats = new();
    private int _baseEliteAttack;
    private int _baseEliteDefense;
    private int _shamanHealCooldown;
    private string? _pendingAchievement;
    private const int MaxLevel = 20; // Fix #183

    private static readonly string[] _playerHitMessages =
    {
        "You strike {0} for {1} damage!",
        "Your blade finds a gap — {1} damage on {0}!",
        "A solid blow connects! {0} takes {1} damage!",
        "You tear through {0}'s guard for {1} damage!",
        "{0} staggers back — {1} damage!"
    };

    private static readonly string[] _warriorHitMessages =
    {
        "You drive your blade deep — {0} takes {1} damage!",
        "A bone-crunching blow! {0} reels back — {1} damage!",
        "You hammer through {0}'s guard with brute force — {1} damage!",
        "Pure power behind the swing — {0} staggers for {1} damage!",
        "You crash into {0} like a battering ram — {1} damage!"
    };

    private static readonly string[] _mageHitMessages =
    {
        "Arcane force tears through {0} — {1} damage!",
        "Eldritch energy crackles as it strikes {0} for {1} damage!",
        "You channel raw magic into a focused bolt — {1} damage to {0}!",
        "Reality bends around your attack — {0} takes {1} damage!",
        "Your spell finds its mark — {1} crackling damage to {0}!"
    };

    private static readonly string[] _rogueHitMessages =
    {
        "You dart in for a precise cut — {0} takes {1} damage!",
        "A lightning-quick strike to the weak point — {1} damage!",
        "You find the gap in {0}'s defenses — {1} damage!",
        "Quick as shadow — {0} barely registers the blow until it hurts. {1} damage!",
        "A surgical strike — {0} bleeds from a wound it didn't see coming. {1} damage!"
    };

    private static readonly string[] _playerMissMessages =
    {
        "{0} sidesteps your attack!",
        "Your blow glances off harmlessly.",
        "You swing wide — {0} ducks back!",
        "{0} twists away at the last moment!",
        "Your strike finds nothing but air."
    };

    private static readonly string[] _warriorMissMessages =
    {
        "You swing with power but {0} isn't where you thought!",
        "Too slow — {0} sidesteps your heavy blow!"
    };

    private static readonly string[] _mageMissMessages =
    {
        "Your spell fizzles at the last moment.",
        "The incantation slips — {0} escapes unscathed!"
    };

    private static readonly string[] _rogueMissMessages =
    {
        "{0} anticipates your angle — the strike finds nothing.",
        "You dart in but {0} reads your movement!"
    };

    private static readonly string[] _critMessages =
    {
        "💥 Critical hit! You slam {0} for {1} damage!",
        "💥 Devastating blow! {1} damage to {0}!",
        "💥 Perfect strike — {1} crushing damage!",
        "💥 You find the weak point! {1} damage on {0}!"
    };

    private static readonly string[] _warriorCritMessages =
    {
        "💥 CRUSHING BLOW! You put your entire body into it — {1} devastating damage to {0}!",
        "💥 SHATTERING STRIKE! {0} is sent reeling — {1} damage!"
    };

    private static readonly string[] _mageCritMessages =
    {
        "💥 ARCANE SURGE! Your spell overloads and detonates — {1} damage on {0}!",
        "💥 CRITICAL RESONANCE! The magic tears through {0} for {1} damage!"
    };

    private static readonly string[] _rogueCritMessages =
    {
        "💥 VITAL STRIKE! You find the perfect spot — {1} piercing damage to {0}!",
        "💥 BACKSTAB! {0} never saw it coming — {1} damage!"
    };

    private static readonly string[] _enemyHitMessages =
    {
        "{0} strikes you for {1} damage!",
        "{0} lands a hit — {1} damage!",
        "You take {1} damage from {0}'s attack!",
        "{0}'s blow connects! {1} damage!",
        "You fail to dodge — {0} deals {1} damage!"
    };

    private static readonly string[] _playerDodgeMessages =
    {
        "You dodge {0}'s attack!",
        "You sidestep {0}'s blow just in time!",
        "{0} swings and misses — you're too quick!",
        "You slip past {0}'s strike!"
    };

    /// <summary>
    /// Initialises a new <see cref="CombatEngine"/> with the required display and input
    /// services, optional event bus, and optional pre-seeded subsystem instances for
    /// deterministic testing.
    /// </summary>
    /// <param name="display">The display service used to render all combat output.</param>
    /// <param name="input">
    /// The input reader used to receive player choices during combat.
    /// Defaults to <see cref="ConsoleInputReader"/> when <see langword="null"/>.
    /// </param>
    /// <param name="rng">
    /// The random-number generator used for hit/dodge/crit rolls.
    /// A new instance is created when <see langword="null"/>.
    /// </param>
    /// <param name="events">
    /// Optional event bus for broadcasting game-wide events such as combat end.
    /// </param>
    /// <param name="statusEffects">
    /// Optional pre-configured status-effect manager; a default instance is created
    /// when <see langword="null"/>.
    /// </param>
    /// <param name="abilities">
    /// Optional pre-configured ability manager; a default instance is created when
    /// <see langword="null"/>.
    /// </param>
    /// <param name="narration">
    /// Optional narration service used to pick varied combat messages; a default instance
    /// sharing <paramref name="rng"/> is created when <see langword="null"/>.
    /// </param>
    public CombatEngine(IDisplayService display, IInputReader? input = null, Random? rng = null, GameEvents? events = null, StatusEffectManager? statusEffects = null, AbilityManager? abilities = null, NarrationService? narration = null, InventoryManager? inventoryManager = null)
    {
        _display = display;
        _input = input ?? new ConsoleInputReader();
        _rng = rng ?? new Random();
        _events = events;
        _statusEffects = statusEffects ?? new StatusEffectManager(display);
        _abilities = abilities ?? new AbilityManager();
        _narration = narration ?? new NarrationService(_rng);
        _inventoryManager = inventoryManager ?? new InventoryManager(display);
    }

    /// <summary>
    /// Replaces only the last occurrence of <paramref name="find"/> in <paramref name="source"/>.
    /// Safe because damage values always appear at the end of narration strings.
    /// </summary>
    private static string ReplaceLastOccurrence(string source, string find, string replace)
    {
        int lastIndex = source.LastIndexOf(find);
        if (lastIndex < 0) return source;
        return source.Substring(0, lastIndex) + replace + source.Substring(lastIndex + find.Length);
    }

    /// <summary>
    /// Colorizes damage numbers in combat messages. Damage in red, healing in green,
    /// crits in yellow + bold.
    /// </summary>
    private string ColorizeDamage(string message, int damage, bool isCrit = false, bool isHealing = false)
    {
        var damageStr = damage.ToString();
        var coloredDamage = isHealing 
            ? ColorCodes.Colorize(damageStr, ColorCodes.Green)
            : ColorCodes.Colorize(damageStr, ColorCodes.BrightRed);
        
        if (isCrit)
        {
            // Crits get bold yellow wrapper
            return ColorCodes.Colorize(ReplaceLastOccurrence(message, damageStr, coloredDamage), ColorCodes.Yellow + ColorCodes.Bold);
        }
        
        return ReplaceLastOccurrence(message, damageStr, coloredDamage);
    }

    /// <summary>
    /// Runs a complete combat encounter between <paramref name="player"/> and
    /// <paramref name="enemy"/>, looping through player/enemy turns until one side
    /// is defeated or the player successfully flees. Handles ambush enemies, boss
    /// phase-two enrage and charged attacks, status-effect tick processing, mana
    /// regeneration, ability menus, loot drops, and XP/level-up on victory.
    /// </summary>
    /// <param name="player">The player character participating in the fight.</param>
    /// <param name="enemy">The enemy the player is fighting.</param>
    /// <returns>
    /// <see cref="CombatResult.Won"/> if the enemy was defeated,
    /// <see cref="CombatResult.Fled"/> if the player escaped, or
    /// <see cref="CombatResult.PlayerDied"/> if the player's HP reached zero.
    /// </returns>
    public CombatResult RunCombat(Player player, Enemy enemy, RunStats? stats = null)
    {
        if (stats != null) _stats = stats;

        // Restore any status effects persisted on the player (e.g., across save/load)
        foreach (var ae in player.ActiveEffects)
            _statusEffects.Apply(player, ae.Effect, ae.RemainingTurns);
        player.ActiveEffects.Clear();

        _display.ShowCombatStart(enemy);
        _display.ShowEnemyArt(enemy);
        _display.ShowCombatEntryFlags(enemy);
        
        if (enemy is DungeonBoss)
        {
            foreach (var line in BossNarration.GetIntro(enemy.Name))
                _display.ShowCombat(line);
        }
        else
        {
            _display.ShowCombat(_narration.Pick(EnemyNarration.GetIntros(enemy.Name), enemy.Name));
        }
        _turnLog.Clear();
        _baseEliteAttack = enemy.Attack;
        _baseEliteDefense = enemy.Defense;
        _shamanHealCooldown = 0;
        _abilities.ResetCooldowns(); // Fix #190: clear cooldowns from previous combat

        // Ambush: Mimic gets a free first strike before the player can act
        if (enemy.IsAmbush)
        {
            _display.ShowCombatMessage($"It's a {enemy.Name}! You've been ambushed!");
            PerformEnemyTurn(player, enemy);
            if (player.HP <= 0) return CombatResult.PlayerDied;
        }
        
        while (true)
        {
            // Fix #167: capture stun state BEFORE ProcessTurnStart decrements durations
            bool playerStunnedThisTurn = _statusEffects.HasEffect(player, StatusEffect.Stun);
            bool enemyStunnedThisTurn  = _statusEffects.HasEffect(enemy,  StatusEffect.Stun);

            _statusEffects.ProcessTurnStart(player);
            _statusEffects.ProcessTurnStart(enemy);

            // Fix #210: player death has priority over enemy death in simultaneous-tick kills
            if (player.HP <= 0) return CombatResult.PlayerDied;
            // Fix #209: guard against enraging/acting on an enemy killed by a DoT tick
            if (enemy.HP <= 0) break;

            int manaRegen = player.Skills.IsUnlocked(Skill.ManaFlow) ? 15 : 10;
            // Ley Conduit passive — +5 mana regeneration/turn
            if (player.Skills.IsUnlocked(Skill.LeyConduit))
                manaRegen += 5;
            player.RestoreMana(manaRegen);
            _abilities.TickCooldowns();
            
            // Decrement Last Stand turns
            if (player.LastStandTurns > 0)
            {
                player.LastStandTurns--;
                if (player.LastStandTurns == 0)
                    _display.ShowCombatMessage("Your Last Stand effect has ended.");
            }

            // Boss Phase 2: check enrage
            if (enemy is DungeonBoss boss)
            {
                var wasEnraged = boss.IsEnraged;
                boss.CheckEnrage();
                if (!wasEnraged && boss.IsEnraged)
                    _display.ShowCombatMessage("⚠ The boss ENRAGES! Its attack has increased by 50%!");
            }
            
            if (enemy.HP <= 0)
            {
                ShowDeathNarration(enemy);
                HandleLootAndXP(player, enemy);
                return CombatResult.Won;
            }
            
            if (player.HP <= 0) return CombatResult.PlayerDied;
            
            // Fix #167/#187: use pre-ProcessTurnStart stun state; message printed only here
            if (playerStunnedThisTurn)
            {
                _display.ShowCombatMessage("You are stunned and cannot act this turn!");
                PerformEnemyTurn(player, enemy, enemyStunnedThisTurn);
                if (player.HP <= 0) return CombatResult.PlayerDied;
                continue;
            }
            
            if (_pendingAchievement != null)
            {
                _display.ShowMessage($"{Systems.ColorCodes.Bold}{Systems.ColorCodes.Yellow}{_pendingAchievement}{Systems.ColorCodes.Reset}");
                _pendingAchievement = null;
            }
            
            _display.ShowCombatStatus(player, enemy, 
                _statusEffects.GetActiveEffects(player), 
                _statusEffects.GetActiveEffects(enemy));
            ShowRecentTurns();
            ShowCombatMenu(player);
            var choice = (_input.ReadLine() ?? string.Empty).Trim().ToUpperInvariant();
            
            if (choice == "F" || choice == "FLEE")
            {
                if (_rng.NextDouble() < 0.5)
                {
                    _display.ShowMessage("You fled successfully!");
                    _statusEffects.Clear(player);
                    _statusEffects.Clear(enemy);
                    player.ActiveEffects.Clear();
                    player.ResetComboPoints();
                    player.LastStandTurns = 0;
                    player.EvadeNextAttack = false;
                    player.ActiveMinions.Clear();
                    player.ActiveTraps.Clear();
                    player.TrapTriggeredThisCombat = false;
                    return CombatResult.Fled;
                }
                else
                {
                    _display.ShowMessage("You failed to flee!");
                    PerformEnemyTurn(player, enemy, enemyStunnedThisTurn);
                    if (player.HP <= 0) return CombatResult.PlayerDied;
                    continue;
                }
            }
            
            if (choice == "B" || choice == "ABILITY")
            {
                var abilityResult = HandleAbilityMenu(player, enemy);
                if (abilityResult == AbilityMenuResult.Cancel)
                    continue;
                if (abilityResult == AbilityMenuResult.Used)
                {
                    if (enemy.HP <= 0)
                    {
                        ShowDeathNarration(enemy);
                        HandleLootAndXP(player, enemy);
                        return CombatResult.Won;
                    }
                    PerformEnemyTurn(player, enemy, enemyStunnedThisTurn);
                    if (player.HP <= 0) return CombatResult.PlayerDied;
                    continue;
                }
            }
            else if (choice == "A" || choice == "ATTACK")
            {
                PerformPlayerAttack(player, enemy);
                
                if (enemy.HP <= 0)
                {
                    ShowDeathNarration(enemy);
                    HandleLootAndXP(player, enemy);
                    return CombatResult.Won;
                }
                
                PerformEnemyTurn(player, enemy, enemyStunnedThisTurn);
                if (player.HP <= 0) return CombatResult.PlayerDied;
            }
            else
            {
                // Fix #211: invalid input does not grant the enemy a free attack
                _display.ShowError("Invalid choice. [A]ttack, [B]ability, or [F]lee.");
                continue;
            }
        }

        // Enemy died from a DoT tick at the start of the turn (break from loop above)
        ShowDeathNarration(enemy);
        HandleLootAndXP(player, enemy);
        return CombatResult.Won;
    }
    
    private void ShowCombatMenu(Player player)
    {
        _display.ShowMessage("[A]ttack [B]ability [F]lee");
        var unlockedAbilities = _abilities.GetUnlockedAbilities(player);
        if (unlockedAbilities.Any())
        {
            var manaLine = $"Mana: {player.Mana}/{player.MaxMana}";
            
            // Rogue: Show Combo Points
            if (player.Class == PlayerClass.Rogue)
            {
                var comboDots = new string('●', player.ComboPoints) + new string('○', 5 - player.ComboPoints);
                manaLine += $"  ⚡ Combo: {comboDots}";
            }
            
            // Mage: Show ManaShield status
            if (player.Class == PlayerClass.Mage && player.IsManaShieldActive)
            {
                manaLine += " [SHIELD ACTIVE]";
            }
            
            _display.ShowMessage(manaLine);
        }
    }

    /// <summary>
    /// Displays the last three entries from the combat turn log, giving the player a
    /// concise summary of recent exchanges before they choose their next action.
    /// </summary>
    private void ShowRecentTurns()
    {
        var recent = _turnLog.Count > 3 ? _turnLog.Skip(_turnLog.Count - 3).ToList() : _turnLog;
        if (recent.Count == 0) return;

        _display.ShowMessage("─── Recent turns ───");
        foreach (var turn in recent)
        {
            string line;
            if (turn.IsDodge)
                line = $"  {turn.Actor}: {turn.Action} → {ColorCodes.Gray}dodged{ColorCodes.Reset}";
            else if (turn.IsCrit)
                line = $"  {turn.Actor}: {turn.Action} → {ColorCodes.Bold}{ColorCodes.Yellow}CRIT{ColorCodes.Reset} {ColorCodes.BrightRed}{turn.Damage}{ColorCodes.Reset} dmg";
            else
                line = $"  {turn.Actor}: {turn.Action} → {ColorCodes.BrightRed}{turn.Damage}{ColorCodes.Reset} dmg";

            if (turn.StatusApplied != null)
                line += $" [{ColorCodes.Green}{turn.StatusApplied}{ColorCodes.Reset}]";

            _display.ShowMessage(line);
        }
    }
    
    private AbilityMenuResult HandleAbilityMenu(Player player, Enemy enemy)
    {
        var unlocked = _abilities.GetUnlockedAbilities(player);
        if (!unlocked.Any())
        {
            _display.ShowMessage("You haven't unlocked any abilities yet!");
            return AbilityMenuResult.Cancel;
        }
        
        _display.ShowMessage("\n=== Abilities ===");
        // Fix #194: use a sequential display counter so shown indices have no gaps
        var displayToAbility = new Dictionary<int, Ability>();
        int displayIndex = 1;
        foreach (var ability in unlocked)
        {
            if (_abilities.IsOnCooldown(ability.Type))
            {
                var cooldown = _abilities.GetCooldown(ability.Type);
                _display.ShowColoredMessage($" (Cooldown: {cooldown} turns) {ability.Name} - {ability.Description} (Cost: {ability.ManaCost} MP, CD: {ability.CooldownTurns} turns)", ColorCodes.Gray);
            }
            else if (player.Mana < ability.ManaCost)
            {
                _display.ShowColoredMessage($" (Need {ability.ManaCost} mana) {ability.Name} - {ability.Description} (Cost: {ability.ManaCost} MP, CD: {ability.CooldownTurns} turns)", ColorCodes.Red);
            }
            else
            {
                displayToAbility[displayIndex] = ability;
                _display.ShowColoredMessage($" [{displayIndex}] {ability.Name} - {ability.Description} (Cost: {ability.ManaCost} MP) — ready", ColorCodes.Green + ColorCodes.Bold);
                displayIndex++;
            }
        }
        _display.ShowMessage("[C]ancel");
        
        var choice = (_input.ReadLine() ?? string.Empty).Trim().ToUpperInvariant();
        if (choice == "C" || choice == "CANCEL")
            return AbilityMenuResult.Cancel;
        
        if (int.TryParse(choice, out int selectedIndex) && displayToAbility.TryGetValue(selectedIndex, out var selectedAbility))
        {
            var hpBeforeAbility = enemy.HP;
            var result = _abilities.UseAbility(player, enemy, selectedAbility.Type, _statusEffects, _display);
            
            if (result == UseAbilityResult.Success)
            {
                _display.ShowMessage($"{ColorCodes.Bold}{ColorCodes.Yellow}[{selectedAbility.Name} activated — {selectedAbility.Description}]{ColorCodes.Reset}");
                // Bug #111: track ability damage in run stats
                if (enemy.HP < hpBeforeAbility)
                    _stats.DamageDealt += hpBeforeAbility - enemy.HP;
                return AbilityMenuResult.Used;
            }
            
            _display.ShowMessage($"Cannot use ability: {result}");
            return AbilityMenuResult.Cancel;
        }
        
        _display.ShowMessage("Invalid choice!");
        return AbilityMenuResult.Cancel;
    }
    
    private void PerformPlayerAttack(Player player, Enemy enemy)
    {
        // Use flat dodge chance for enemies like Wraith, otherwise DEF-based
        bool dodged = enemy.FlatDodgeChance >= 0
            ? _rng.NextDouble() < enemy.FlatDodgeChance
            : RollDodge(enemy.Defense);

        if (dodged)
        {
            var missPool = player.Class switch {
                PlayerClass.Warrior => _warriorMissMessages,
                PlayerClass.Mage    => _mageMissMessages,
                PlayerClass.Rogue   => _rogueMissMessages,
                _                   => _playerMissMessages
            };
            _display.ShowCombatMessage(_narration.Pick(missPool, enemy.Name));
            _turnLog.Add(new CombatTurn("You", "Attack", 0, false, true, null));
        }
        else
        {
            var playerDmg = Math.Max(1, player.Attack - enemy.Defense);
            var isCrit = RollCrit();
            if (isCrit)
            {
                playerDmg *= 2;
            }
            // Warrior passive: +5% damage when HP < 50%
            if (player.Class == PlayerClass.Warrior && player.HP < player.MaxHP / 2.0)
                playerDmg = (int)(playerDmg * 1.05);
            // Bug #86: PowerStrike skill passive â +15% damage
            if (player.Skills.IsUnlocked(Skill.PowerStrike))
                playerDmg = Math.Max(1, (int)(playerDmg * 1.15));
            // Berserker's Edge passive: +10% damage per 25% HP missing
            if (player.Skills.IsUnlocked(Skill.BerserkersEdge))
            {
                var hpPercent = (float)player.HP / player.MaxHP;
                var multiplier = 1.0f;
                if (hpPercent <= 0.25f) multiplier = 1.40f;      // 75% missing = +40%
                else if (hpPercent <= 0.50f) multiplier = 1.30f; // 50% missing = +30%
                else if (hpPercent <= 0.75f) multiplier = 1.20f; // 25% missing = +20%
                else multiplier = 1.10f;                         // <25% missing = +10%
                playerDmg = Math.Max(1, (int)(playerDmg * multiplier));
            }
            // Last Stand damage boost — +50% damage
            if (player.LastStandTurns > 0)
                playerDmg = Math.Max(1, (int)(playerDmg * 1.5f));
            
            enemy.HP -= playerDmg;
            _stats.DamageDealt += playerDmg;
            var hitPool = player.Class switch {
                PlayerClass.Warrior => _warriorHitMessages,
                PlayerClass.Mage    => _mageHitMessages,
                PlayerClass.Rogue   => _rogueHitMessages,
                _                   => _playerHitMessages
            };
            var critPool = player.Class switch {
                PlayerClass.Warrior => _warriorCritMessages,
                PlayerClass.Mage    => _mageCritMessages,
                PlayerClass.Rogue   => _rogueCritMessages,
                _                   => _critMessages
            };
            if (isCrit)
                _display.ShowCombatMessage(ColorizeDamage(_narration.Pick(critPool, enemy.Name, playerDmg), playerDmg, true));
            else
                _display.ShowCombatMessage(ColorizeDamage(_narration.Pick(hitPool, enemy.Name, playerDmg), playerDmg));

            string? statusApplied = null;
            // Bug #110: bleed-on-hit from equipped weapon (10% chance, 3 turns)
            if (player.EquippedWeaponAppliesBleed && _rng.NextDouble() < 0.10)
            {
                _statusEffects.Apply(enemy, StatusEffect.Bleed, 3);
                statusApplied = "Bleed";
                _display.ShowColoredCombatMessage($"{enemy.Name} is bleeding!", ColorCodes.Red);
            }
            _turnLog.Add(new CombatTurn("You", "Attack", playerDmg, isCrit, false, statusApplied));
        }
    }
    
    private void PerformEnemyTurn(Player player, Enemy enemy, bool stunOverride = false)
    {
        // Fix #167: accept pre-ProcessTurnStart stun state so 1-turn stuns are honoured
        if (stunOverride || _statusEffects.HasEffect(enemy, StatusEffect.Stun))
        {
            _display.ShowCombatMessage($"{enemy.Name} is stunned and cannot act!");
            return;
        }

        // Goblin Shaman: try to heal when below 50% HP (once every 3 turns)
        if (_shamanHealCooldown > 0) _shamanHealCooldown--;
        if (enemy is GoblinShaman shaman && shaman.HP < shaman.MaxHP / 2 && _shamanHealCooldown == 0)
        {
            _shamanHealCooldown = 3;
            _display.ShowCombatMessage("The shaman mutters a guttural incantation. Dark energy knits its wounds closed!");
            int heal = (int)(shaman.MaxHP * 0.20);
            shaman.HP = Math.Min(shaman.MaxHP, shaman.HP + heal);
            _display.ShowCombatMessage(ColorizeDamage($"The {shaman.Name} channels dark magic and heals for {heal}!", heal, false, true));
            _display.ShowCombatMessage($"({shaman.Name} HP: {shaman.HP}/{shaman.MaxHP})");
            return; // skip normal attack this turn
        }
        // Troll: regenerates 5% max HP each turn
        if (enemy is Troll troll)
        {
            _display.ShowCombatMessage("The troll's wounds close before your eyes with a wet, nauseating sound.");
            int regen = Math.Max(1, (int)(troll.MaxHP * 0.05));
            troll.HP = Math.Min(troll.MaxHP, troll.HP + regen);
            _display.ShowCombatMessage(ColorizeDamage($"The Troll regenerates {regen} HP!", regen, false, true));
        }

        // Boss telegraphed charge: resolve previous charge or set new one
        if (enemy is DungeonBoss boss)
        {
            if (boss.IsCharging)
            {
                boss.IsCharging = false;
                boss.ChargeActive = true;
            }
            else if (_rng.Next(100) < 30)
            {
                boss.IsCharging = true;
                _display.ShowCombatMessage($"⚠ {enemy.Name} is charging a powerful attack! Prepare to defend!");
                return; // warn turn — no damage this turn
            }
        }

        // Elite special abilities: 15% chance per turn to use a random elite move
        if (enemy.IsElite && _rng.Next(100) < 15)
        {
            switch (_rng.Next(3))
            {
                case 0:
                    _display.ShowCombatMessage("The elite lands a stunning blow — your head rings!");
                    _statusEffects.Apply(player, StatusEffect.Stun, 1);
                    return;
                case 1:
                    _display.ShowCombatMessage("The elite roars and attacks with renewed fury!");
                    enemy.Attack = Math.Min((int)(enemy.Attack * 1.1), _baseEliteAttack * 2);
                    break;
                case 2:
                    _display.ShowCombatMessage("The elite lets out a war cry, bolstering its own resolve!");
                    enemy.Defense = Math.Min((int)(enemy.Defense * 1.1), _baseEliteDefense * 2);
                    break;
            }
        }
        
        // Bug #107: clear ChargeActive before the dodge roll so it resets whether hit or missed
        bool wasCharged = false;
        if (enemy is DungeonBoss pendingChargeBoss && pendingChargeBoss.ChargeActive)
        {
            pendingChargeBoss.ChargeActive = false;
            wasCharged = true;
        }

        // Bug #85: include equipment and class dodge bonuses for the player
        PerformTrapTriggerPhase(player, enemy);
        if (enemy.HP <= 0) return;

        if (player.EvadeNextAttack)
        {
            player.EvadeNextAttack = false;
            _display.ShowCombatMessage("The enemy's attack finds only shadows.");
            _turnLog.Add(new CombatTurn(enemy.Name, "Attack", 0, false, true, null));
        }
        else if (RollPlayerDodge(player))
        {
            _display.ShowCombatMessage(_narration.Pick(_playerDodgeMessages, enemy.Name));
            _turnLog.Add(new CombatTurn(enemy.Name, "Attack", 0, false, true, null));
        }
        else
        {
            var playerEffDef = player.Defense + _statusEffects.GetStatModifier(player, "Defense"); // Fix #197: +50% DEF from Fortified via stat modifier system
            var enemyDmg = Math.Max(1, enemy.Attack - playerEffDef);

            // Apply charge multiplier (3x)
            if (wasCharged)
            {
                enemyDmg *= 3;
                _display.ShowCombatMessage($"⚡ {enemy.Name} unleashes the charged attack!");
            }

            var isCrit = RollCrit();
            if (isCrit)
            {
                enemyDmg *= 2;
                _display.ShowCombatMessage(ColorCodes.Colorize("💥 Critical hit!", ColorCodes.BrightRed + ColorCodes.Bold));
            }
            // BattleHardened skill passive — 5% damage reduction (matches skill description)
            if (player.Skills.IsUnlocked(Skill.BattleHardened))
                enemyDmg = Math.Max(1, (int)(enemyDmg * 0.95f));
            // Iron Constitution passive — 5% damage reduction
            if (player.Skills.IsUnlocked(Skill.IronConstitution))
                enemyDmg = Math.Max(1, (int)(enemyDmg * 0.95f));
            // Last Stand damage reduction — 75% damage reduction
            if (player.LastStandTurns > 0)
                enemyDmg = Math.Max(1, (int)(enemyDmg * 0.25f));
            
            // Mana Shield absorption
            if (player.IsManaShieldActive)
            {
                var manaLost = (int)(enemyDmg * 1.5);
                if (player.Mana >= manaLost)
                {
                    player.Mana -= manaLost;
                    _display.ShowCombatMessage($"The mana shield absorbs the blow! ({manaLost} mana lost)");
                    _turnLog.Add(new CombatTurn(enemy.Name, "Attack", 0, isCrit, false, null));
                    return; // No HP damage taken
                }
                else
                {
                    // Shield breaks, take remaining as HP damage
                    var remainingDamage = enemyDmg - (player.Mana * 2 / 3); // reverse calculation
                    player.Mana = 0;
                    player.IsManaShieldActive = false;
                    enemyDmg = Math.Max(1, remainingDamage);
                    _display.ShowCombatMessage("Your mana shield shatters!");
                }
            }
            
            player.TakeDamage(enemyDmg);
            _stats.DamageTaken += enemyDmg;
            _display.ShowCombatMessage(ColorizeDamage(_narration.Pick(_enemyHitMessages, enemy.Name, enemyDmg), enemyDmg));

            // TODO: Phase 4 — Check player.ShouldTriggerUndyingWill() and apply Regen status if needed
            // Requires tracking "once per combat" flag for UndyingWill passive

            string? statusApplied = null;

            // Poison on hit (e.g. Goblin Shaman poisons the player when it lands a hit)
            if (enemy.AppliesPoisonOnHit)
            {
                _statusEffects.Apply(player, StatusEffect.Poison, 3);
                statusApplied = "Poison";
            }

            // Lifesteal (e.g. Vampire Lord)
            if (enemy.LifestealPercent > 0)
            {
                var heal = (int)(enemyDmg * enemy.LifestealPercent);
                if (heal > 0)
                {
                    _display.ShowCombatMessage($"{enemy.Name} channels stolen life force, growing stronger!"); // Fix #206
                    enemy.HP = Math.Min(enemy.MaxHP, enemy.HP + heal);
                    _display.ShowCombatMessage($"{enemy.Name} drains {heal} HP!");
                }
            }

            _turnLog.Add(new CombatTurn(enemy.Name, "Attack", enemyDmg, isCrit, false, statusApplied));
        }
    }
    
    private void PerformMinionAttackPhase(Player player, Enemy enemy)
    {
        foreach (var minion in player.ActiveMinions.Where(m => m.HP > 0).ToList())
        {
            var dmg = Math.Max(1, minion.ATK - enemy.Defense);
            enemy.HP -= dmg;
            _stats.DamageDealt += dmg;
            _display.ShowCombatMessage(ColorizeDamage($"{minion.AttackFlavorText} ({dmg} damage)", dmg));
            if (enemy.HP <= 0) break;
        }
        player.ActiveMinions.RemoveAll(m => m.HP <= 0);
    }

    private void PerformTrapTriggerPhase(Player player, Enemy enemy)
    {
        var trap = player.ActiveTraps.FirstOrDefault(t => !t.Triggered);
        if (trap == null) return;

        trap.Triggered = true;
        player.TrapTriggeredThisCombat = true;
        var dmg = Math.Max(1, (int)(player.Attack * trap.DamagePercent));
        enemy.HP -= dmg;
        _stats.DamageDealt += dmg;
        _display.ShowCombatMessage(ColorizeDamage($"{trap.FlavorText} ({dmg} damage)", dmg));

        if (trap.AppliedStatus.HasValue && enemy.HP > 0)
        {
            _statusEffects.Apply(enemy, trap.AppliedStatus.Value, trap.StatusDuration);
            _display.ShowColoredCombatMessage($"{enemy.Name} is affected by {trap.AppliedStatus.Value}!", ColorCodes.Green);
        }
    }

    private void HandleLootAndXP(Player player, Enemy enemy)
    {
        player.LastKilledEnemyHp = enemy.MaxHP;

        if (enemy.LootTable != null)
        {
        var loot = enemy.LootTable.RollDrop(enemy, player.Level);
        if (loot.Gold > 0)
        {
            player.AddGold(loot.Gold);
            _stats.GoldCollected += loot.Gold;
            _display.ShowGoldPickup(loot.Gold, player.Gold);
        }
        if (loot.Item != null)
        {
            if (!_inventoryManager.TryAddItem(player, loot.Item))
                _display.ShowMessage($"{Systems.ColorCodes.Red}❌ Inventory full — {loot.Item.Name} was lost!{Systems.ColorCodes.Reset}\n   Drop something to make room.");
            else
                _display.ShowLootDrop(loot.Item, player, enemy.IsElite);
        }
        }
        
        player.AddXP(enemy.XPValue);
        var xpToNext = 100 * player.Level;
        _display.ShowMessage($"You gained {enemy.XPValue} XP. (Total: {player.XP}/{xpToNext} to next level)");
        CheckLevelUp(player);
        
        _stats.EnemiesDefeated++;
        
        // Check combat-relevant achievement milestones
        if (_events != null)
        {
            if (_stats.EnemiesDefeated == 10)
                _events.RaiseAchievementUnlocked("Slayer", "Defeated 10 enemies");
            else if (_stats.EnemiesDefeated == 25)
                _events.RaiseAchievementUnlocked("Veteran", "Defeated 25 enemies");
            else if (_stats.EnemiesDefeated == 50)
                _events.RaiseAchievementUnlocked("Champion", "Defeated 50 enemies");
        }
        
        _events?.RaiseCombatEnded(player, enemy, CombatResult.Won);
        _statusEffects.Clear(player);
        _statusEffects.Clear(enemy);
        player.ActiveEffects.Clear();
        player.ResetComboPoints();
        player.LastStandTurns = 0;
        player.EvadeNextAttack = false;
        player.ActiveMinions.Clear();
        player.ActiveTraps.Clear();
        player.TrapTriggeredThisCombat = false;
    }
    
    private void CheckLevelUp(Player player)
    {
        while (player.Level < MaxLevel && player.XP / 100 + 1 > player.Level)
        {
            player.LevelUp();
            _display.ShowMessage($"LEVEL UP! You are now level {player.Level}!");

            // Every 2 levels, offer a trait bonus
            if (player.Level % 2 == 0)
            {
                _display.ShowLevelUpChoice(player);
                var traitChoice = (_input.ReadLine() ?? "1").Trim();
                switch (traitChoice)
                {
                    case "2":
                        player.ModifyAttack(2);
                        _display.ShowMessage("You feel stronger! +2 Attack");
                        break;
                    case "3":
                        player.ModifyDefense(2);
                        _display.ShowMessage("You feel tougher! +2 Defense");
                        break;
                    default:
                        player.MaxHP += 5;
                        player.HP = Math.Min(player.HP + 5, player.MaxHP);
                        _display.ShowMessage("You feel healthier! +5 Max HP");
                        break;
                }
            }
        }
    }
    
    private void ShowDeathNarration(Enemy enemy)
    {
        if (enemy is DungeonBoss)
            _display.ShowCombat(BossNarration.GetDeath(enemy.Name));
        else
            _display.ShowCombat(_narration.Pick(EnemyNarration.GetDeaths(enemy.Name), enemy.Name));
    }

    private bool RollDodge(int defense)
    {
        var dodgeChance = defense / (double)(defense + 20);
        return _rng.NextDouble() < dodgeChance;
    }

    /// <summary>
    /// Rolls a dodge check for the player, incorporating base-defense probability,
    /// equipped-item dodge bonuses (<see cref="Player.DodgeBonus"/>), class bonus
    /// (<see cref="Player.ClassDodgeBonus"/>), and the Swiftness skill passive (+5%).
    /// </summary>
    /// <param name="player">The player attempting to dodge an incoming attack.</param>
    /// <returns><see langword="true"/> if the dodge succeeds; otherwise <see langword="false"/>.</returns>
    private bool RollPlayerDodge(Player player)
    {
        // Bug #85: add flat equipment and class bonuses on top of DEF-based chance
        float dodgeChance = player.Defense / (player.Defense + 20f)
                          + player.DodgeBonus
                          + player.ClassDodgeBonus;
        // Bug #86: Swiftness skill passive — +5% dodge chance
        if (player.Skills.IsUnlocked(Skill.Swiftness))
            dodgeChance += 0.05f;
        // Quick Reflexes passive — +5% dodge chance
        if (player.Skills.IsUnlocked(Skill.QuickReflexes))
            dodgeChance += 0.05f;
        dodgeChance = Math.Min(dodgeChance, 0.95f);
        return _rng.NextDouble() < dodgeChance;
    }
    
    private bool RollCrit()
    {
        return _rng.NextDouble() < 0.15;
    }
}

/// <summary>
/// Indicates the outcome of the player's interaction with the in-combat ability
/// selection menu, so the combat loop knows whether to advance to the enemy's
/// turn or to re-display the main combat prompt.
/// </summary>
public enum AbilityMenuResult
{
    /// <summary>
    /// The player dismissed the menu without using an ability, either by choosing
    /// "Cancel" explicitly or by providing an invalid selection.
    /// </summary>
    Cancel,

    /// <summary>
    /// The player successfully activated an ability, consuming the required mana
    /// and triggering its effect; the enemy turn should now follow.
    /// </summary>
    Used
}
