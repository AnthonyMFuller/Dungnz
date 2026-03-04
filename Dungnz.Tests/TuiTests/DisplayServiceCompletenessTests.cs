using System.Reflection;
using Dungnz.Display;
using Dungnz.Display.Tui;
using FluentAssertions;

namespace Dungnz.Tests.TuiTests;

/// <summary>
/// Completeness tests comparing TerminalGuiDisplayService with SpectreDisplayService
/// to verify that the TUI implementation has all the same method signatures.
/// </summary>
public class DisplayServiceCompletenessTests
{
    [Fact]
    public void TerminalGuiDisplayService_HasSameMethodCount_AsSpectreDisplayService()
    {
        // Arrange
        var spectreType = typeof(SpectreDisplayService);
        var terminalGuiType = typeof(TerminalGuiDisplayService);

        var spectreMethods = spectreType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        var terminalGuiMethods = terminalGuiType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // Filter out object methods (Equals, GetHashCode, etc.)
        var spectreCount = spectreMethods.Count(m => !m.IsSpecialName);
        var terminalGuiCount = terminalGuiMethods.Count(m => !m.IsSpecialName);

        // Assert
        terminalGuiCount.Should().BeGreaterOrEqualTo(spectreCount,
            "TerminalGuiDisplayService should implement at least as many methods as SpectreDisplayService");
    }

    [Fact]
    public void TerminalGuiDisplayService_ImplementsAllIDisplayServiceMethods()
    {
        // Arrange
        var interfaceType = typeof(IDisplayService);
        var implementationType = typeof(TerminalGuiDisplayService);
        var interfaceMethods = interfaceType.GetMethods();

        // Act & Assert
        foreach (var interfaceMethod in interfaceMethods)
        {
            var parameterTypes = interfaceMethod.GetParameters().Select(p => p.ParameterType).ToArray();
            var implementedMethod = implementationType.GetMethod(
                interfaceMethod.Name,
                BindingFlags.Public | BindingFlags.Instance,
                null,
                parameterTypes,
                null
            );

            implementedMethod.Should().NotBeNull(
                $"TerminalGuiDisplayService should implement {interfaceMethod.Name}");

            implementedMethod!.ReturnType.Should().Be(interfaceMethod.ReturnType,
                $"{interfaceMethod.Name} should have return type {interfaceMethod.ReturnType.Name}");
        }
    }

    [Fact]
    public void SpectreDisplayService_ImplementsAllIDisplayServiceMethods()
    {
        // Arrange
        var interfaceType = typeof(IDisplayService);
        var implementationType = typeof(SpectreDisplayService);
        var interfaceMethods = interfaceType.GetMethods();

        // Act & Assert
        foreach (var interfaceMethod in interfaceMethods)
        {
            var parameterTypes = interfaceMethod.GetParameters().Select(p => p.ParameterType).ToArray();
            var implementedMethod = implementationType.GetMethod(
                interfaceMethod.Name,
                BindingFlags.Public | BindingFlags.Instance,
                null,
                parameterTypes,
                null
            );

            implementedMethod.Should().NotBeNull(
                $"SpectreDisplayService should implement {interfaceMethod.Name}");

            implementedMethod!.ReturnType.Should().Be(interfaceMethod.ReturnType,
                $"{interfaceMethod.Name} should have return type {interfaceMethod.ReturnType.Name}");
        }
    }

    [Fact]
    public void TerminalGuiDisplayService_HasMatchingMethodSignatures_WithSpectre()
    {
        // Arrange
        var interfaceMethods = typeof(IDisplayService).GetMethods();
        var spectreType = typeof(SpectreDisplayService);
        var terminalGuiType = typeof(TerminalGuiDisplayService);

        // Act & Assert
        foreach (var interfaceMethod in interfaceMethods)
        {
            var parameterTypes = interfaceMethod.GetParameters().Select(p => p.ParameterType).ToArray();

            var spectreMethod = spectreType.GetMethod(
                interfaceMethod.Name,
                BindingFlags.Public | BindingFlags.Instance,
                null,
                parameterTypes,
                null
            );

            var terminalGuiMethod = terminalGuiType.GetMethod(
                interfaceMethod.Name,
                BindingFlags.Public | BindingFlags.Instance,
                null,
                parameterTypes,
                null
            );

            spectreMethod.Should().NotBeNull($"SpectreDisplayService should have {interfaceMethod.Name}");
            terminalGuiMethod.Should().NotBeNull($"TerminalGuiDisplayService should have {interfaceMethod.Name}");

            if (spectreMethod != null && terminalGuiMethod != null)
            {
                terminalGuiMethod.ReturnType.Should().Be(spectreMethod.ReturnType,
                    $"{interfaceMethod.Name} should have matching return types");

                var spectreParams = spectreMethod.GetParameters();
                var terminalGuiParams = terminalGuiMethod.GetParameters();

                terminalGuiParams.Length.Should().Be(spectreParams.Length,
                    $"{interfaceMethod.Name} should have same parameter count");

                for (int i = 0; i < spectreParams.Length; i++)
                {
                    terminalGuiParams[i].ParameterType.Should().Be(spectreParams[i].ParameterType,
                        $"{interfaceMethod.Name} parameter {i} should match");
                    // Note: Parameter names may differ between implementations
                    // (e.g., "color" vs "valueColor") - only type matters
                }
            }
        }
    }

    [Theory]
    [InlineData("ShowTitle")]
    [InlineData("ShowRoom")]
    [InlineData("ShowCombat")]
    [InlineData("ShowCombatStatus")]
    [InlineData("ShowCombatMessage")]
    [InlineData("ShowPlayerStats")]
    [InlineData("ShowInventory")]
    [InlineData("ShowMessage")]
    [InlineData("ShowError")]
    [InlineData("ShowHelp")]
    [InlineData("ShowCommandPrompt")]
    [InlineData("ShowMap")]
    [InlineData("ShowEquipment")]
    [InlineData("ShowEnhancedTitle")]
    public void TerminalGuiDisplayService_ImplementsMethod(string methodName)
    {
        // Arrange
        var type = typeof(TerminalGuiDisplayService);

        // Act
        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);

        // Assert
        method.Should().NotBeNull($"{methodName} should be implemented");
    }

    [Theory]
    [InlineData("ShowInventoryAndSelect")]
    [InlineData("ShowShopAndSelect")]
    [InlineData("ShowSellMenuAndSelect")]
    [InlineData("ShowCombatMenuAndSelect")]
    [InlineData("ShowLevelUpChoiceAndSelect")]
    [InlineData("ShowShrineMenuAndSelect")]
    [InlineData("ShowShopWithSellAndSelect")]
    [InlineData("ShowCraftMenuAndSelect")]
    [InlineData("ShowTrapChoiceAndSelect")]
    [InlineData("ShowForgottenShrineMenuAndSelect")]
    [InlineData("ShowContestedArmoryMenuAndSelect")]
    [InlineData("ShowAbilityMenuAndSelect")]
    [InlineData("ShowCombatItemMenuAndSelect")]
    [InlineData("ShowEquipMenuAndSelect")]
    [InlineData("ShowUseMenuAndSelect")]
    [InlineData("ShowTakeMenuAndSelect")]
    [InlineData("ShowConfirmMenu")]
    [InlineData("ShowSkillTreeMenu")]
    public void TerminalGuiDisplayService_ImplementsInputMethod(string methodName)
    {
        // Arrange
        var type = typeof(TerminalGuiDisplayService);

        // Act
        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);

        // Assert
        method.Should().NotBeNull($"{methodName} (input-coupled method) should be implemented");
    }

    [Fact]
    public void TerminalGuiDisplayService_AllMethodsHaveTheSameAccessibility_AsSpectre()
    {
        // Arrange
        var interfaceMethods = typeof(IDisplayService).GetMethods();
        var spectreType = typeof(SpectreDisplayService);
        var terminalGuiType = typeof(TerminalGuiDisplayService);

        // Act & Assert
        foreach (var interfaceMethod in interfaceMethods)
        {
            var parameterTypes = interfaceMethod.GetParameters().Select(p => p.ParameterType).ToArray();

            var spectreMethod = spectreType.GetMethod(
                interfaceMethod.Name,
                BindingFlags.Public | BindingFlags.Instance,
                null,
                parameterTypes,
                null
            );

            var terminalGuiMethod = terminalGuiType.GetMethod(
                interfaceMethod.Name,
                BindingFlags.Public | BindingFlags.Instance,
                null,
                parameterTypes,
                null
            );

            if (spectreMethod != null && terminalGuiMethod != null)
            {
                terminalGuiMethod.IsPublic.Should().Be(spectreMethod.IsPublic,
                    $"{interfaceMethod.Name} accessibility should match");
            }
        }
    }

    [Fact]
    public void BothDisplayServices_ImplementSameInterfaceMethods()
    {
        // Arrange
        var spectreInterfaces = typeof(SpectreDisplayService).GetInterfaces();
        var terminalGuiInterfaces = typeof(TerminalGuiDisplayService).GetInterfaces();

        // Act & Assert
        spectreInterfaces.Should().Contain(typeof(IDisplayService));
        terminalGuiInterfaces.Should().Contain(typeof(IDisplayService));
    }

    [Fact]
    public void TerminalGuiDisplayService_DoesNotHaveExtraPublicMethods()
    {
        // Arrange
        var interfaceMethods = typeof(IDisplayService).GetMethods().Select(m => m.Name).ToHashSet();
        var implementationType = typeof(TerminalGuiDisplayService);
        var implementedMethods = implementationType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName) // Exclude property getters/setters
            .Select(m => m.Name)
            .ToList();

        // Act
        var extraMethods = implementedMethods.Where(m => !interfaceMethods.Contains(m)).ToList();

        // Assert
        extraMethods.Should().BeEmpty(
            "TerminalGuiDisplayService should only have methods defined in IDisplayService (found extra: {0})",
            string.Join(", ", extraMethods));
    }

    [Fact]
    public void SpectreDisplayService_DoesNotHaveExtraPublicMethods()
    {
        // Arrange
        var interfaceMethods = typeof(IDisplayService).GetMethods().Select(m => m.Name).ToHashSet();
        var implementationType = typeof(SpectreDisplayService);
        var implementedMethods = implementationType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName) // Exclude property getters/setters
            .Select(m => m.Name)
            .ToList();

        // Act
        var extraMethods = implementedMethods.Where(m => !interfaceMethods.Contains(m)).ToList();

        // Assert
        extraMethods.Should().BeEmpty(
            "SpectreDisplayService should only have methods defined in IDisplayService (found extra: {0})",
            string.Join(", ", extraMethods));
    }
}
