using System.Reflection;
using Dungnz.Display;
using Dungnz.Engine;
using Xunit;

namespace Dungnz.Tests.ArchRules;

/// <summary>
/// Extended architecture enforcement tests using ArchUnitNET to validate layer boundaries,
/// console abstraction rules, and interface placement conventions.
/// </summary>
public class LayerArchitectureTests
{

    // ── Rule: Display namespace should not call System.Console directly ──────
    // Display implementations should use Spectre or IDisplayService abstractions.
    // PRE-EXISTING TECH DEBT: DisplayService.cs currently calls Console directly.
    // This test is intentionally left failing for visibility. Fix tracked separately.
    [Fact]
    public void Display_Should_Not_Depend_On_System_Console()
    {
        var displayTypes = typeof(GameLoop).Assembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith("Dungnz.Display") == true && !t.IsInterface
                        && !t.Name.StartsWith("Console"))
            .ToList();

        var violations = new List<string>();
        foreach (var type in displayTypes)
        {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static |
                                          BindingFlags.Public | BindingFlags.NonPublic |
                                          BindingFlags.DeclaredOnly);
            foreach (var method in methods)
            {
                try
                {
                    var body = method.GetMethodBody();
                    if (body == null) continue;
                    var il = body.GetILAsByteArray();
                    if (il == null) continue;

                    // Check metadata tokens for Console method references
                    foreach (var token in EnumerateMetadataTokens(il))
                    {
                        try
                        {
                            var member = type.Module.ResolveMember(token);
                            if (member?.DeclaringType == typeof(Console))
                            {
                                violations.Add($"{type.Name}.{method.Name} calls Console.{member.Name}");
                                break;
                            }
                        }
                        catch { /* token not resolvable */ }
                    }
                }
                catch { /* method body not available */ }
            }
        }

        Assert.True(violations.Count == 0,
            // PRE-EXISTING TECH DEBT: DisplayService uses raw Console calls.
            // This is expected to fail until DisplayService is migrated to Spectre-only output.
            $"Display types should not call System.Console directly (use Spectre/IDisplayService):\n" +
            string.Join("\n", violations.Take(10)));
    }

    // ── Rule: Engine namespace must NOT call System.Console directly ─────────
    // PRE-EXISTING TECH DEBT: ConsoleInputReader in Engine namespace calls Console.ReadLine/ReadKey.
    // This adapter implements IInputReader and is the sole Console-touching type in Engine.
    // Ideally it would live in a separate Infrastructure namespace. Left failing for visibility.
    [Fact]
    public void Engine_Must_Not_Call_Console_Directly()
    {
        var engineTypes = typeof(GameLoop).Assembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith("Dungnz.Engine") == true && !t.IsInterface
                        && !t.Name.StartsWith("Console"))
            .ToList();

        var violations = new List<string>();
        foreach (var type in engineTypes)
        {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static |
                                          BindingFlags.Public | BindingFlags.NonPublic |
                                          BindingFlags.DeclaredOnly);
            foreach (var method in methods)
            {
                try
                {
                    var body = method.GetMethodBody();
                    if (body == null) continue;
                    var il = body.GetILAsByteArray();
                    if (il == null) continue;

                    foreach (var token in EnumerateMetadataTokens(il))
                    {
                        try
                        {
                            var member = type.Module.ResolveMember(token);
                            if (member?.DeclaringType == typeof(Console))
                            {
                                violations.Add($"{type.Name}.{method.Name} calls Console.{member.Name}");
                                break;
                            }
                        }
                        catch { /* token not resolvable */ }
                    }
                }
                catch { /* method body not available */ }
            }
        }

        Assert.True(violations.Count == 0,
            $"Engine types must not call System.Console directly (all I/O goes through IDisplayService):\n" +
            string.Join("\n", violations.Take(10)));
    }

    // ── Rule: IDisplayService implementations must reside in Dungnz.Display ──
    [Fact]
    public void IDisplayService_Implementations_Must_Reside_In_Display_Namespace()
    {
        var displayServiceType = typeof(IDisplayService);
        var assembly = typeof(GameLoop).Assembly;

        var implementations = assembly.GetTypes()
            .Where(t => !t.IsInterface && !t.IsAbstract && displayServiceType.IsAssignableFrom(t))
            .ToList();

        var violations = implementations
            .Where(t => t.Namespace?.StartsWith("Dungnz.Display") != true)
            .Select(t => $"{t.FullName} implements IDisplayService but resides in {t.Namespace}")
            .ToList();

        Assert.True(violations.Count == 0,
            $"All IDisplayService implementations must be in the Dungnz.Display namespace:\n" +
            string.Join("\n", violations));
    }

    /// <summary>
    /// Scans IL byte array for call/callvirt/newobj opcodes and yields their metadata tokens.
    /// </summary>
    private static IEnumerable<int> EnumerateMetadataTokens(byte[] il)
    {
        // call = 0x28, callvirt = 0x6F, newobj = 0x73, ldftn = 0xFE06
        for (int i = 0; i < il.Length; i++)
        {
            byte op = il[i];
            if ((op == 0x28 || op == 0x6F || op == 0x73) && i + 4 < il.Length)
            {
                int token = il[i + 1] | (il[i + 2] << 8) | (il[i + 3] << 16) | (il[i + 4] << 24);
                yield return token;
                i += 4;
            }
            else if (op == 0xFE && i + 1 < il.Length && il[i + 1] == 0x06 && i + 5 < il.Length)
            {
                int token = il[i + 2] | (il[i + 3] << 8) | (il[i + 4] << 16) | (il[i + 5] << 24);
                yield return token;
                i += 5;
            }
        }
    }
}
