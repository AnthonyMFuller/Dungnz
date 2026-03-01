namespace Dungnz.Systems;

using System.Text.Json;
using Microsoft.Extensions.Logging;

/// <summary>
/// Logs completed game sessions to a JSON-lines file for balance analysis and player
/// progression tracking. Each run is appended as a single JSON line to sessions.jsonl.
/// </summary>
public static class SessionLogger
{
    /// <summary>
    /// Appends a session record to the JSONL log file at AppData/Dungnz/sessions/sessions.jsonl.
    /// Never throws — failures are silently swallowed to avoid crashing the game.
    /// </summary>
    /// <param name="stats">The run statistics to log.</param>
    /// <param name="playerWon">True if the player won the run; false if they died.</param>
    public static void LogSession(RunStats stats, bool playerWon)
    {
        try
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Dungnz", "sessions");
            Directory.CreateDirectory(logDir);
            
            var record = new
            {
                timestamp = DateTime.UtcNow.ToString("O"),
                result = playerWon ? "Victory" : "Defeat",
                floor = stats.FloorReached,
                enemiesDefeated = stats.EnemiesDefeated,
                damageDealt = stats.DamageDealt,
                damageTaken = stats.DamageTaken,
                goldCollected = stats.GoldCollected,
                turnsTaken = stats.TurnsTaken,
                abilitiesUsed = stats.AbilitiesUsed,
                deathCause = stats.DeathCause,
                deathEnemy = stats.DeathEnemy
            };
            
            var logFile = Path.Combine(logDir, "sessions.jsonl");
            var json = JsonSerializer.Serialize(record);
            File.AppendAllText(logFile, json + Environment.NewLine);
        }
        catch
        {
            // Never crash the game due to logging failure
        }
    }

    /// <summary>
    /// Logs a balance summary at the end of a run using the structured logger.
    /// Captures gold earned, enemies killed, floor reached, boss kills, and damage dealt.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="sessionStats">The per-session balance metrics.</param>
    /// <param name="outcome">The run outcome: "Victory", "Defeat", or "Quit".</param>
    public static void LogBalanceSummary(ILogger logger, SessionStats sessionStats, string outcome)
    {
        try
        {
            logger.LogInformation(
                "Session summary — Outcome: {Outcome}, EnemiesKilled: {EnemiesKilled}, GoldEarned: {GoldEarned}, FloorsCleared: {FloorsCleared}, BossKills: {BossKills}, DamageDealt: {DamageDealt}",
                outcome,
                sessionStats.EnemiesKilled,
                sessionStats.GoldEarned,
                sessionStats.FloorsCleared,
                sessionStats.BossKills,
                sessionStats.DamageDealt);
        }
        catch
        {
            // Never crash the game due to logging failure
        }
    }
}
