using System.Reflection;
using System.Text.Json.Serialization;
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using Dungnz.Engine;
using Dungnz.Models;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using SystemType = System.Type;
using SystemAssembly = System.Reflection.Assembly;

namespace Dungnz.Tests;

/// <summary>
/// Architecture enforcement tests that validate layer boundaries and prevent
/// common bugs (e.g., missing JsonDerivedType registrations that cause save crashes).
/// </summary>
public class ArchitectureTests
{
    private static readonly Architecture Architecture =
        new ArchLoader().LoadAssemblies(typeof(GameLoop).Assembly).Build();

    // TODO: Re-enable when ArchUnitNET supports NotCallMethod (requires newer version)
    // [Fact]
    // public void Engine_And_Systems_Must_Not_Call_Console_Write_Directly()
    // {
    //     var rule = Types()
    //         .That().ResideInNamespace("Dungnz.Engine")
    //         .Or().ResideInNamespace("Dungnz.Systems")
    //         .Should().NotCallMethod(typeof(Console), nameof(Console.Write))
    //         .AndShould().NotCallMethod(typeof(Console), nameof(Console.WriteLine))
    //         .AndShould().NotCallMethod(typeof(Console), nameof(Console.ReadLine))
    //         .AndShould().NotCallMethod(typeof(Console), nameof(Console.ReadKey));
    //     rule.Check(Architecture);
    // }

    [Fact]
    public void Models_Must_Not_Depend_On_Systems()
    {
        var rule = Types()
            .That().ResideInNamespace("Dungnz.Models")
            .Should().NotDependOnAnyTypesThat().ResideInNamespace("Dungnz.Systems");
        
        rule.Check(Architecture);
    }

    [Fact]
    public void AllEnemySubclasses_MustHave_JsonDerivedTypeAttribute()
    {
        // Every concrete Enemy subclass must be registered on the Enemy base class
        // This prevents the P0 boss serialization crash bug from recurring
        var enemyType = typeof(Enemy);
        var assembly = System.Reflection.Assembly.GetAssembly(enemyType)!;
        
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
