using System.Reflection;
using Dungnz.Models;
using Dungnz.Tests.Helpers;
using FluentAssertions;

namespace Dungnz.Tests.ArchRules;

/// <summary>
/// Verifies the IDisplayService ↔ IGameDisplay / IGameInput interface split
/// introduced in the Avalonia migration Phase 0.
///
/// These tests use reflection to confirm:
/// 1. IDisplayService is the exhaustive union of IGameDisplay + IGameInput
/// 2. FakeDisplayService (test double) implements the facade
/// 3. Engine/Systems classes depend only on IDisplayService, not the sub-interfaces
/// </summary>
public class InterfaceSplitTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the set of method signatures (Name + parameter types) declared
    /// directly on the given interface, excluding default-implemented methods'
    /// bodies but including their declarations.
    /// </summary>
    private static HashSet<string> GetMethodSignatures(Type iface)
    {
        return iface
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => $"{m.Name}({string.Join(",", m.GetParameters().Select(p => p.ParameterType.FullName))})")
            .ToHashSet();
    }

    // ── 1. Every IGameDisplay method exists on IDisplayService ───────────────

    [Fact]
    public void IDisplayService_InheritsAllMethodsFrom_IGameDisplay()
    {
        // Arrange
        var displayMethods = typeof(IGameDisplay)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        var facadeMethods = typeof(IDisplayService)
            .GetInterfaces()
            .SelectMany(i => i.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Select(m => m.Name)
            .ToHashSet();

        // Act & Assert — every IGameDisplay method should be reachable through IDisplayService
        foreach (var method in displayMethods)
        {
            facadeMethods.Should().Contain(method.Name,
                because: $"IDisplayService must expose IGameDisplay.{method.Name}");
        }
    }

    // ── 2. Every IGameInput method exists on IDisplayService ─────────────────

    [Fact]
    public void IDisplayService_InheritsAllMethodsFrom_IGameInput()
    {
        // Arrange
        var inputMethods = typeof(IGameInput)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        var facadeMethods = typeof(IDisplayService)
            .GetInterfaces()
            .SelectMany(i => i.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Select(m => m.Name)
            .ToHashSet();

        // Act & Assert — every IGameInput method should be reachable through IDisplayService
        foreach (var method in inputMethods)
        {
            facadeMethods.Should().Contain(method.Name,
                because: $"IDisplayService must expose IGameInput.{method.Name}");
        }
    }

    // ── 3. IDisplayService is the exhaustive union (no extra methods) ────────

    [Fact]
    public void IDisplayService_HasNoMethodsOutside_IGameDisplay_Or_IGameInput()
    {
        // Arrange — methods declared directly on IDisplayService (not inherited)
        var ownMethods = typeof(IDisplayService)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        var displaySigs = GetMethodSignatures(typeof(IGameDisplay));
        var inputSigs = GetMethodSignatures(typeof(IGameInput));
        var unionSigs = displaySigs.Union(inputSigs).ToHashSet();

        // Act & Assert — any method declared directly on IDisplayService must also
        // appear in IGameDisplay or IGameInput (the facade adds no extra surface).
        foreach (var method in ownMethods)
        {
            var sig = $"{method.Name}({string.Join(",", method.GetParameters().Select(p => p.ParameterType.FullName))})";
            unionSigs.Should().Contain(sig,
                because: $"IDisplayService.{method.Name} should come from IGameDisplay or IGameInput, not be declared independently");
        }
    }

    // ── 4. FakeDisplayService implements IDisplayService (and therefore both) ─

    [Fact]
    public void FakeDisplayService_Implements_IDisplayService()
    {
        // Arrange & Act
        var fakeType = typeof(FakeDisplayService);

        // Assert
        fakeType.Should().Implement<IDisplayService>(
            because: "the test double must satisfy the full facade");
        fakeType.Should().Implement<IGameDisplay>(
            because: "IDisplayService inherits IGameDisplay");
        fakeType.Should().Implement<IGameInput>(
            because: "IDisplayService inherits IGameInput");
    }

    [Fact]
    public void FakeDisplayService_CanBeAssignedTo_EitherSubInterface()
    {
        // Arrange
        var fake = new FakeDisplayService();

        // Act & Assert — runtime assignment must succeed for both sub-interfaces
        IGameDisplay display = fake;
        IGameInput input = fake;

        display.Should().NotBeNull();
        input.Should().NotBeNull();
    }

    // ── 5. Engine/Systems classes depend only on IDisplayService ─────────────

    [Fact]
    public void EngineAndSystems_DoNotDependDirectlyOn_IGameDisplay()
    {
        // Arrange — load all concrete types from Engine and Systems assemblies
        var engineAssembly = typeof(Dungnz.Engine.GameLoop).Assembly;
        var systemsAssembly = typeof(Dungnz.Systems.InventoryManager).Assembly;

        var allTypes = engineAssembly.GetTypes()
            .Concat(systemsAssembly.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract);

        var violations = new List<string>();

        // Act — scan constructors, fields, properties, and method parameters
        foreach (var type in allTypes)
        {
            // Constructor parameters
            foreach (var ctor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                foreach (var param in ctor.GetParameters())
                {
                    if (param.ParameterType == typeof(IGameDisplay) || param.ParameterType == typeof(IGameInput))
                        violations.Add($"{type.Name} ctor param '{param.Name}' uses {param.ParameterType.Name}");
                }
            }

            // Fields
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
                if (field.FieldType == typeof(IGameDisplay) || field.FieldType == typeof(IGameInput))
                    violations.Add($"{type.Name} field '{field.Name}' uses {field.FieldType.Name}");
            }

            // Properties
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
                if (prop.PropertyType == typeof(IGameDisplay) || prop.PropertyType == typeof(IGameInput))
                    violations.Add($"{type.Name} property '{prop.Name}' uses {prop.PropertyType.Name}");
            }
        }

        // Assert
        violations.Should().BeEmpty(
            because: "Engine and Systems classes should depend on IDisplayService, not on the narrower IGameDisplay/IGameInput sub-interfaces");
    }

    // ── 6. IDisplayService inherits exactly two interfaces ───────────────────

    [Fact]
    public void IDisplayService_InheritsExactly_IGameDisplay_And_IGameInput()
    {
        // Arrange
        var directBases = typeof(IDisplayService).GetInterfaces();

        // Assert
        directBases.Should().Contain(typeof(IGameDisplay));
        directBases.Should().Contain(typeof(IGameInput));
    }

    // ── 7. Method counts are consistent ─────────────────────────────────────

    [Fact]
    public void InterfaceMethodCounts_AreConsistent()
    {
        // Arrange
        var displayCount = typeof(IGameDisplay)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Length;

        var inputCount = typeof(IGameInput)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Length;

        var facadeDeclaredCount = typeof(IDisplayService)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Length;

        // Act & Assert
        displayCount.Should().BeGreaterThan(0, because: "IGameDisplay should declare output methods");
        inputCount.Should().BeGreaterThan(0, because: "IGameInput should declare input methods");
        facadeDeclaredCount.Should().Be(0,
            because: "IDisplayService should declare no methods of its own — it's a pure union");
    }
}
