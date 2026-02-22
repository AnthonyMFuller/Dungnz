using Dungnz.Engine;
using Xunit;

namespace Dungnz.Tests.Helpers;

/// <summary>
/// Shared xUnit collection fixture that initialises <see cref="EnemyFactory"/> once
/// for the entire test assembly. All test classes that directly or indirectly rely on
/// <see cref="EnemyFactory"/> must belong to the <c>"EnemyFactory"</c> collection.
/// </summary>
public class EnemyFactoryFixture
{
    /// <summary>
    /// Initialises <see cref="EnemyFactory"/> using the data files copied to the
    /// test output directory.
    /// </summary>
    public EnemyFactoryFixture()
    {
        var enemyPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Data", "enemy-stats.json");
        var itemPath  = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Data", "item-stats.json");
        EnemyFactory.Initialize(enemyPath, itemPath);
    }
}

/// <summary>
/// xUnit collection definition for tests that depend on <see cref="EnemyFactory"/>
/// static state. Sharing this collection ensures <see cref="EnemyFactoryFixture"/>
/// runs exactly once before any test in the collection executes.
/// </summary>
[CollectionDefinition("EnemyFactory")]
public class EnemyFactoryCollection : ICollectionFixture<EnemyFactoryFixture> { }
