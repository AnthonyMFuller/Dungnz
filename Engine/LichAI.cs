using Dungnz.Models;

namespace Dungnz.Engine;

/// <summary>
/// AI for the Lich: undead resurrection mechanic.
/// When Lich HP drops to 0, it resurrects once with partial HP restore
/// and a dramatic message. Subsequent death is permanent.
/// </summary>
public class LichAI : IEnemyAI
{
    private bool _hasResurrected;

    /// <summary>Gets whether the Lich has already used its resurrection ability.</summary>
    public bool HasResurrected => _hasResurrected;

    /// <summary>
    /// Executes Lich turn: primarily performs standard attacks.
    /// The resurrection logic is handled via CheckResurrection method
    /// which should be called externally when HP reaches 0.
    /// </summary>
    public void TakeTurn(Enemy self, Player player, CombatContext context)
    {
        // Lich performs standard attacks during its turn
        LastAction = EnemyAction.Attack;
    }

    /// <summary>
    /// Checks if the Lich can resurrect and performs the resurrection if eligible.
    /// Should be called when the Lich's HP would drop to 0 or below.
    /// Returns true if resurrection occurred, false otherwise.
    /// </summary>
    /// <param name="self">The Lich enemy instance.</param>
    /// <returns>True if resurrection occurred; false if already resurrected or ineligible.</returns>
    public bool CheckResurrection(Enemy self)
    {
        if (_hasResurrected || self.HP > 0)
        {
            return false;
        }

        // Resurrect with 40% of max HP
        int resurrectionHP = (int)(self.MaxHP * 0.40);
        self.HP = resurrectionHP;
        _hasResurrected = true;
        LastAction = EnemyAction.Resurrect;
        ResurrectionHP = resurrectionHP;

        return true;
    }

    /// <summary>The action taken on the last TakeTurn or CheckResurrection call.</summary>
    public EnemyAction LastAction { get; private set; }

    /// <summary>The amount of HP restored during resurrection (0 if not resurrected).</summary>
    public int ResurrectionHP { get; private set; }
}
