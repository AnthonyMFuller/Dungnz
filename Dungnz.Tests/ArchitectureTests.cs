using System.Reflection;
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using Dungnz.Engine;
using Dungnz.Models;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Dungnz.Tests;

/// <summary>
/// Architecture enforcement tests that validate layer boundaries and prevent
/// common bugs (e.g., missing enemy type registrations that cause save crashes).
/// </summary>
public class ArchitectureTests
{
    private static readonly Architecture Architecture =
        new ArchLoader().LoadAssemblies(typeof(GameLoop).Assembly, typeof(Enemy).Assembly).Build();

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
    public void EnemyTypeRegistry_MustRegister_AllConcreteEnemySubclasses()
    {
        // EnemyTypeRegistry must register every concrete Enemy subclass so that
        // polymorphic save/load never silently loses type information.
        var enemyType = typeof(Enemy);

        // Only inspect the main game assembly — test helpers that extend Enemy should not require registration
        var gameAssembly = typeof(GameLoop).Assembly;
        var allSubclasses = gameAssembly.GetTypes()
            .Where(t => t.IsSubclassOf(enemyType) && !t.IsAbstract)
            .ToHashSet();

        var registeredTypes = EnemyTypeRegistry.RegisteredTypes().Values.ToHashSet();

        var missing = allSubclasses.Except(registeredTypes).ToList();

        Assert.True(missing.Count == 0,
            $"The following Enemy subclasses are missing from EnemyTypeRegistry " +
            $"and will cause save crashes: {string.Join(", ", missing.Select(t => t.Name))}");
    }
}
