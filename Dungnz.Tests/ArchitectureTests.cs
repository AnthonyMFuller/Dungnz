using System.Reflection;
using System.Text.Json.Serialization;
using Dungnz.Models;
using Xunit;
using SystemAssembly = System.Reflection.Assembly;

namespace Dungnz.Tests;

/// <summary>
/// Architecture enforcement tests that prevent common bugs
/// (e.g., missing JsonDerivedType registrations that cause save crashes).
/// </summary>
public class ArchitectureTests
{
    [Fact]
    public void AllEnemySubclasses_MustHave_JsonDerivedTypeAttribute()
    {
        // Every concrete Enemy subclass must be registered on the Enemy base class
        // This prevents the P0 boss serialization crash bug from recurring
        var enemyType = typeof(Enemy);
        var assembly = SystemAssembly.GetAssembly(enemyType)!;
        
        var subclasses = assembly.GetTypes()
            .Where(t => t.IsSubclassOf(enemyType) && !t.IsAbstract)
            .ToList();
        
        var registeredTypes = enemyType
            .GetCustomAttributes<JsonDerivedTypeAttribute>()
            .Select(a => a.DerivedType)
            .ToHashSet();
        
        var missing = subclasses.Where(t => !registeredTypes.Contains(t)).ToList();
        
        Assert.True(missing.Count == 0,
            $"The following Enemy subclasses are missing [JsonDerivedType] on Enemy base class " +
            $"and will cause save crashes: {string.Join(", ", missing.Select(t => t.Name))}");
    }
}
