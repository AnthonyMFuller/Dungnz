using Xunit;

// Disable parallel test execution across the entire assembly.
// Several test classes mutate shared static state (LootTable tier pools,
// StatusEffectRegistry, AffixRegistry, EnemyFactory) and the suite is
// fast enough (~1-2 s) that sequential execution has negligible cost.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
