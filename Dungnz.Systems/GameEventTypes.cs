using Dungnz.Models;

namespace Dungnz.Systems;

/// <summary>Fired when a combat encounter ends (win, loss, or flee).</summary>
public record OnCombatEnd(Player Player, Enemy Enemy, CombatResult Result) : IGameEvent;

/// <summary>Fired when the player takes damage from any source.</summary>
public record OnPlayerDamaged(Player Player, int DamageAmount, string Source) : IGameEvent;

/// <summary>Fired when an enemy is killed by the player.</summary>
public record OnEnemyKilled(Player Player, Enemy Enemy) : IGameEvent;

/// <summary>Fired when the player enters a new room.</summary>
public record OnRoomEntered(Player Player, Room Room, Room? PreviousRoom) : IGameEvent;
