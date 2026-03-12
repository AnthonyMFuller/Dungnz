using System.Reflection;
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using Dungnz.Data;
using Dungnz.Display;
using Dungnz.Engine;
using Dungnz.Systems;
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
        new ArchLoader().LoadAssemblies(
            typeof(GameLoop).Assembly,          // Dungnz.Engine
            typeof(Enemy).Assembly,             // Dungnz.Models
            typeof(InventoryManager).Assembly,  // Dungnz.Systems
            typeof(ConsoleDisplayService).Assembly, // Dungnz.Display
            typeof(CombatNarration).Assembly    // Dungnz.Data
        ).Build();

    [Fact]
    public void Engine_And_Systems_Must_Not_Call_Console_Write_Directly()
    {
        // Custom enforcement: scan IL for bare Console method calls.
        // IDisplayService pattern is the only valid path for I/O.
        var engineAssembly = typeof(GameLoop).Assembly;
        var systemsAssembly = typeof(InventoryManager).Assembly;
        
        var violations = new List<string>();
        
        foreach (var assembly in new[] { engineAssembly, systemsAssembly })
        {
            foreach (var type in assembly.GetTypes())
            {
                // Skip compiler-generated types (async state machines, etc.)
                if (type.Name.Contains("<") || type.Name.Contains(">"))
                    continue;
                    
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    try
                    {
                        var methodBody = method.GetMethodBody();
                        if (methodBody == null) continue;
                        
                        var il = methodBody.GetILAsByteArray();
                        if (il == null) continue;
                        
                        // Scan IL bytes for call/callvirt opcodes followed by Console method tokens
                        var module = method.Module;
                        for (int i = 0; i < il.Length - 5; i++)
                        {
                            // call = 0x28, callvirt = 0x6F
                            if (il[i] == 0x28 || il[i] == 0x6F)
                            {
                                var token = BitConverter.ToInt32(il, i + 1);
                                try
                                {
                                    var calledMethod = module.ResolveMethod(token);
                                    if (calledMethod?.DeclaringType == typeof(Console) &&
                                        (calledMethod.Name == "Write" || 
                                         calledMethod.Name == "WriteLine" || 
                                         calledMethod.Name == "ReadLine" || 
                                         calledMethod.Name == "ReadKey"))
                                    {
                                        violations.Add($"{type.FullName}.{method.Name} calls Console.{calledMethod.Name}");
                                    }
                                }
                                catch
                                {
                                    // Token resolution can fail for generic methods, extern, etc. - skip
                                }
                            }
                        }
                    }
                    catch
                    {
                        // GetMethodBody can throw for abstract/extern methods - skip
                    }
                }
            }
        }
        
        Assert.True(violations.Count == 0,
            $"Production code must not call Console I/O methods directly. Use IDisplayService instead.\n" +
            $"Violations:\n{string.Join("\n", violations)}");
    }

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

        var registeredTypes = Dungnz.Systems.EnemyTypeRegistry.RegisteredTypes().Values.ToHashSet();

        var missing = allSubclasses.Except(registeredTypes).ToList();

        Assert.True(missing.Count == 0,
            $"The following Enemy subclasses are missing from EnemyTypeRegistry " +
            $"and will cause save crashes: {string.Join(", ", missing.Select(t => t.Name))}");
    }
}
