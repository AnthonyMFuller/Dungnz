using Xunit;

namespace Dungnz.Tests.Helpers;

/// <summary>
/// xUnit collection definition for tests that share <see cref="Dungnz.Models.LootTable"/>
/// static tier-pool state. <c>DisableParallelization = true</c> prevents any other
/// collection from running concurrently with "LootTableTests", eliminating the race
/// condition on <c>LootTable._sharedTier1/2/3</c>.
/// </summary>
[CollectionDefinition("LootTableTests", DisableParallelization = true)]
public class LootTableTestsCollection { }
