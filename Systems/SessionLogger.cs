namespace Dungnz.Systems;

using System.Text.Json;

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
}
