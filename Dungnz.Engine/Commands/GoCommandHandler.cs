namespace Dungnz.Engine.Commands;

using Dungnz.Models;
using Dungnz.Systems;
using Microsoft.Extensions.Logging;

internal sealed class GoCommandHandler : ICommandHandler
{
    private static readonly string[] _postCombatLines =
    {
        "The room falls silent. Nothing moves but the dust settling around the fallen {0}.",
        "You stand over {0}'s body, catching your breath. The dungeon feels momentarily less hostile.",
        "The echo of combat fades. {0} is dead. You survived.",
        "Silence returns. {0} won't be troubling anyone else."
    };

    private static readonly string[] _spikeHazardLines =
    {
        "Pressure plates click underfoot. Razor spikes lance from the walls! ({0} damage)",
        "The floor drops a half-inch — then a volley of iron spikes erupts from the stone! ({0} damage)"
    };

    private static readonly string[] _poisonHazardLines =
    {
        "A hissing sound, then green mist floods the chamber. Your lungs burn! ({0} damage)",
        "Pressure triggers a vial of alchemical poison — the fumes are immediate and agonising. ({0} damage)"
    };

    private static readonly string[] _fireHazardLines =
    {
        "A gout of magical fire roars from runes on the floor — you're caught in the blast! ({0} damage)",
        "The floor glows red. Then the fire trap activates with a WHOMP that singes your eyebrows. ({0} damage)"
    };

    public void Handle(string argument, CommandContext context)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            context.TurnConsumed = false;
            context.Display.ShowError("Go where? Specify a direction (north, south, east, west).");
            return;
        }

        Direction direction;
        switch (argument.ToLowerInvariant())
        {
            case "north":
            case "n":
                direction = Direction.North;
                break;
            case "south":
            case "s":
                direction = Direction.South;
                break;
            case "east":
            case "e":
                direction = Direction.East;
                break;
            case "west":
            case "w":
                direction = Direction.West;
                break;
            default:
                context.TurnConsumed = false;
                context.Display.ShowError($"Invalid direction: {argument}");
                return;
        }

        if (!context.CurrentRoom.Exits.TryGetValue(direction, out var nextRoom))
        {
            context.TurnConsumed = false;
            context.Display.ShowError("You can't go that way.");
            return;
        }

        // Move to new room
        var previousRoom = context.CurrentRoom;
        context.PreviousRoom = previousRoom;
        context.CurrentRoom = nextRoom;

        // ~15% chance of a brief atmospheric flavor message before the room description
        if (context.Narration.Chance(GameConstants.AtmosphericNarrationChance))
            context.Display.ShowMessage(context.Narration.Pick(AmbientEvents.ForFloor(context.CurrentFloor)));

        // Show revisit flavor when returning to an already-explored room
        if (context.CurrentRoom.Visited)
        {
            context.Display.ShowMessage(context.Narration.Pick(RoomStateNarration.RevisitedRoom));
            context.CurrentRoom.State = RoomState.Revisited;
        }

        context.Display.ShowRoom(context.CurrentRoom);
        
        // Display dynamic room entry narration based on room state
        var narrationState = DetermineRoomNarrationState(context.CurrentRoom);
        var narrationLine = context.Narration.GetRoomEntryNarration(narrationState);
        if (!string.IsNullOrEmpty(narrationLine))
        {
            context.Display.ShowMessage(narrationLine);
        }
        
        context.CurrentRoom.Visited = true;
        context.Events?.RaiseRoomEntered(context.Player, context.CurrentRoom, previousRoom);
        context.Logger.LogDebug("Player entered room at {RoomId}", context.CurrentRoom.Id);

        // Apply environmental hazard damage
        if (context.CurrentRoom.Hazard != HazardType.None)
        {
            var dmg = context.CurrentRoom.Hazard switch {
                HazardType.Spike => GameConstants.HazardDamageSpike,
                HazardType.Poison => GameConstants.HazardDamagePoison,
                HazardType.Fire => GameConstants.HazardDamageFire,
                _ => 0
            };
            context.Player.TakeDamage(dmg);
            context.Stats.DamageTaken += dmg;
            string hazardMsg = context.CurrentRoom.Hazard switch
            {
                HazardType.Spike  => context.Narration.Pick(_spikeHazardLines, dmg),
                HazardType.Poison => context.Narration.Pick(_poisonHazardLines, dmg),
                HazardType.Fire   => context.Narration.Pick(_fireHazardLines, dmg),
                _                 => $"You trigger a hazard and take {dmg} damage!"
            };
            context.Display.ShowMessage($"⚠ {hazardMsg} HP: {context.Player.HP}/{context.Player.MaxHP}");
            context.Display.ShowPlayerStats(context.Player);
            if (context.Player.HP <= 0)
            {
                context.ExitRun("a dungeon trap");
                return;
            }
        }

        // Notify about merchant if present
        if (context.CurrentRoom.Merchant != null)
        {
            context.Display.ShowMessage("🛒 A merchant is here! Type SHOP to browse, or SELL to sell items.");
        }

        // Check for enemy encounter
        if (context.CurrentRoom.Enemy != null && !context.CurrentRoom.Enemy.IsDead)
        {
            var killerName = context.CurrentRoom.Enemy.Name;
            context.Logger.LogInformation("Combat started with {EnemyName}", context.CurrentRoom.Enemy.Name);
            var result = context.Combat.RunCombat(context.Player, context.CurrentRoom.Enemy, context.Stats);
            context.Logger.LogInformation("Combat ended: {Result}", result);

            if (result == CombatResult.PlayerDied)
            {
                context.ExitRun(killerName);
                return;
            }

            if (result == CombatResult.Won)
            {
                context.SessionStats.EnemiesKilled++;
                if (context.CurrentRoom.Enemy is Systems.Enemies.DungeonBoss
                    || context.CurrentRoom.Enemy is Systems.Enemies.ArchlichSovereign
                    || context.CurrentRoom.Enemy is Systems.Enemies.AbyssalLeviathan
                    || context.CurrentRoom.Enemy is Systems.Enemies.InfernalDragon)
                    context.SessionStats.BossKills++;
                var enemyName = context.CurrentRoom.Enemy!.Name;
                context.CurrentRoom.Enemy = null;
                context.CurrentRoom.State = RoomState.Cleared;
                context.Display.ShowRoom(context.CurrentRoom);
                context.Display.ShowMessage(context.Narration.Pick(_postCombatLines, enemyName));
                context.Display.ShowMessage(context.Narration.Pick(RoomStateNarration.ClearedRoom));
                context.Display.ShowPlayerStats(context.Player);
            }

            if (result == CombatResult.Fled)
            {
                context.Display.ShowMessage("You flee back to the previous room!");
                context.CurrentRoom = previousRoom;
                return;
            }
        }

        if (context.Player.HP < context.Player.MaxHP * GameConstants.CriticalHpThreshold)
            context.Logger.LogWarning("Player HP critically low: {HP}/{MaxHP}", context.Player.HP, context.Player.MaxHP);

        // Check win/floor condition
        if (context.CurrentRoom.IsExit && context.CurrentRoom.Enemy == null)
        {
            if (context.CurrentFloor >= GameConstants.FinalFloor)
            {
                context.Stats.FinalLevel = context.Player.Level;
                context.Stats.TimeElapsed = DateTime.UtcNow - context.RunStart;
                context.Display.ShowVictory(context.Player, context.CurrentFloor, context.Stats);
                context.Display.ShowMessage($"Difficulty: {context.GetDifficultyName()}");
                if (context.Seed.HasValue) context.Display.ShowMessage($"Run seed: {context.Seed.Value}");
                context.Stats.Display(context.Display.ShowMessage);
                context.RecordRunEnd(true, null);
                context.GameOver = true;
                return;
            }
            else
            {
                var clearedVariant = DungeonVariant.ForFloor(context.CurrentFloor);
                if (!string.IsNullOrEmpty(clearedVariant.ExitMessage))
                    context.Display.ShowMessage(clearedVariant.ExitMessage);
                context.Display.ShowMessage($"You cleared Floor {context.CurrentFloor}! Type DESCEND to go deeper.");
            }
        }

        // Prompt for shrine if present and not yet used
        if (context.CurrentRoom.HasShrine && !context.CurrentRoom.ShrineUsed)
        {
            context.Display.ShowMessage("✨ There is a Shrine in this room. Type USE SHRINE to interact.");
        }

        // Auto-trigger PetrifiedLibrary on first entry
        if (context.CurrentRoom.Type == RoomType.PetrifiedLibrary && !context.CurrentRoom.SpecialRoomUsed)
        {
            context.CurrentRoom.SpecialRoomUsed = true;
            context.HandlePetrifiedLibrary();
        }

        // Auto-trigger TrapRoom on first entry
        if (context.CurrentRoom.Type == RoomType.TrapRoom && !context.CurrentRoom.SpecialRoomUsed)
            context.HandleTrapRoom();

        // Prompt for ContestedArmory
        if (context.CurrentRoom.Type == RoomType.ContestedArmory && !context.CurrentRoom.SpecialRoomUsed)
        {
            context.Display.ShowMessage("⚔ Trapped weapons line the walls. (USE ARMORY to approach)");
        }
    }

    private static RoomNarrationState DetermineRoomNarrationState(Room room)
    {
        // Priority order: Merchant > Shrine > Boss > Cleared > ActiveEnemies > FirstVisit
        
        if (room.Merchant != null)
            return RoomNarrationState.Merchant;
        
        if (room.HasShrine)
            return RoomNarrationState.Shrine;
        
        // Check if this is a boss room
        if (room.Enemy != null && IsBossEnemy(room.Enemy))
            return RoomNarrationState.Boss;
        
        // Check if room is cleared
        if (room.IsCleared)
            return RoomNarrationState.Cleared;
        
        // Check for active enemies
        if (room.Enemy != null && !room.Enemy.IsDead)
        {
            return room.WasVisited ? RoomNarrationState.ActiveEnemies : RoomNarrationState.FirstVisit;
        }
        
        // Default to FirstVisit for fresh rooms
        return room.WasVisited ? RoomNarrationState.ActiveEnemies : RoomNarrationState.FirstVisit;
    }

    private static bool IsBossEnemy(Enemy enemy)
    {
        var enemyType = enemy.GetType();
        return enemyType.Name is "DungeonBoss" 
            or "ArchlichSovereign" 
            or "AbyssalLeviathan" 
            or "InfernalDragon";
    }
}
