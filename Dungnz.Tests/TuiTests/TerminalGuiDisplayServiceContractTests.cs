using System.Reflection;
using Dungnz.Display;
using Dungnz.Display.Tui;
using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using FluentAssertions;

namespace Dungnz.Tests.TuiTests;

/// <summary>
/// Contract tests for TerminalGuiDisplayService verifying that every IDisplayService
/// method is implemented (not throwing NotImplementedException).
/// </summary>
/// <remarks>
/// These tests verify the contract without requiring Terminal.Gui initialization.
/// They use reflection to detect NotImplementedException patterns and verify method existence.
/// </remarks>
public class TerminalGuiDisplayServiceContractTests
{
    [Fact]
    public void TerminalGuiDisplayService_ImplementsIDisplayService()
    {
        // Arrange & Act
        var implementsInterface = typeof(TerminalGuiDisplayService)
            .GetInterfaces()
            .Contains(typeof(IDisplayService));

        // Assert
        implementsInterface.Should().BeTrue("TerminalGuiDisplayService must implement IDisplayService");
    }

    [Fact]
    public void IDisplayService_AllMethodsAreImplemented()
    {
        // Arrange
        var interfaceMethods = typeof(IDisplayService).GetMethods();
        var implementationType = typeof(TerminalGuiDisplayService);

        // Act & Assert
        foreach (var method in interfaceMethods)
        {
            var implementedMethod = implementationType.GetMethod(
                method.Name,
                BindingFlags.Public | BindingFlags.Instance,
                null,
                method.GetParameters().Select(p => p.ParameterType).ToArray(),
                null
            );

            implementedMethod.Should().NotBeNull(
                $"method {method.Name} should be implemented in TerminalGuiDisplayService");

            // Check that the method is not just throwing NotImplementedException
            var methodBody = implementedMethod!.GetMethodBody();
            if (methodBody != null)
            {
                // If we can get the IL, check it's not trivially throwing
                methodBody.GetILAsByteArray()!.Length.Should().BeGreaterThan(2,
                    $"method {method.Name} should have a real implementation (not just 'throw')");
            }
        }
    }

    [Fact]
    public void TerminalGuiDisplayService_ShowTitle_DoesNotThrowNotImplementedException()
    {
        // Arrange
        var layout = new TuiLayout();
        var service = new TerminalGuiDisplayService(layout);

        // Act
        Action act = () => service.ShowTitle();

        // Assert
        act.Should().NotThrow<NotImplementedException>();
    }

    [Fact]
    public void TerminalGuiDisplayService_ShowRoom_DoesNotThrowNotImplementedException()
    {
        // Arrange
        var layout = new TuiLayout();
        var service = new TerminalGuiDisplayService(layout);
        var room = new Room { Description = "A test room", Type = RoomType.Standard };

        // Act
        Action act = () => service.ShowRoom(room);

        // Assert
        act.Should().NotThrow<NotImplementedException>();
    }

    [Fact]
    public void TerminalGuiDisplayService_ShowMessage_DoesNotThrowNotImplementedException()
    {
        // Arrange
        var layout = new TuiLayout();
        var service = new TerminalGuiDisplayService(layout);

        // Act
        Action act = () => service.ShowMessage("Test message");

        // Assert
        act.Should().NotThrow<NotImplementedException>();
    }

    [Fact]
    public void TerminalGuiDisplayService_ShowError_DoesNotThrowNotImplementedException()
    {
        // Arrange
        var layout = new TuiLayout();
        var service = new TerminalGuiDisplayService(layout);

        // Act
        Action act = () => service.ShowError("Test error");

        // Assert
        act.Should().NotThrow<NotImplementedException>();
    }

    [Fact]
    public void TerminalGuiDisplayService_ShowHelp_DoesNotThrowNotImplementedException()
    {
        // Arrange
        var layout = new TuiLayout();
        var service = new TerminalGuiDisplayService(layout);

        // Act
        Action act = () => service.ShowHelp();

        // Assert
        act.Should().NotThrow<NotImplementedException>();
    }

    [Fact]
    public void TerminalGuiDisplayService_ShowCommandPrompt_DoesNotThrowNotImplementedException()
    {
        // Arrange
        var layout = new TuiLayout();
        var service = new TerminalGuiDisplayService(layout);

        // Act
        Action act = () => service.ShowCommandPrompt(null);

        // Assert
        act.Should().NotThrow<NotImplementedException>();
    }

    [Theory]
    [InlineData("ShowTitle")]
    [InlineData("ShowHelp")]
    [InlineData("ShowEnhancedTitle")]
    public void TerminalGuiDisplayService_VoidMethods_Exist(string methodName)
    {
        // Arrange
        var type = typeof(TerminalGuiDisplayService);

        // Act
        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);

        // Assert
        method.Should().NotBeNull($"{methodName} should be implemented");
        method!.ReturnType.Should().Be(typeof(void), $"{methodName} should return void");
    }

    [Theory]
    [InlineData("ReadPlayerName", typeof(string))]
    [InlineData("SelectDifficulty", typeof(Difficulty))]
    [InlineData("ShowIntroNarrative", typeof(bool))]
    public void TerminalGuiDisplayService_ReturningMethods_HaveCorrectSignature(string methodName, Type expectedReturnType)
    {
        // Arrange
        var type = typeof(TerminalGuiDisplayService);

        // Act
        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);

        // Assert
        method.Should().NotBeNull($"{methodName} should be implemented");
        method!.ReturnType.Should().Be(expectedReturnType, $"{methodName} should return {expectedReturnType.Name}");
    }

    [Theory]
    [InlineData("ShowShopAndSelect")]
    [InlineData("ShowSellMenuAndSelect")]
    [InlineData("ShowLevelUpChoiceAndSelect")]
    [InlineData("ShowShrineMenuAndSelect")]
    [InlineData("ShowShopWithSellAndSelect")]
    [InlineData("ShowTrapChoiceAndSelect")]
    [InlineData("ShowForgottenShrineMenuAndSelect")]
    [InlineData("ShowContestedArmoryMenuAndSelect")]
    [InlineData("ShowCraftMenuAndSelect")]
    public void TerminalGuiDisplayService_IntReturningMenuMethods_Exist(string methodName)
    {
        // Arrange
        var type = typeof(TerminalGuiDisplayService);

        // Act
        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);

        // Assert
        method.Should().NotBeNull($"{methodName} should be implemented");
        method!.ReturnType.Should().Be(typeof(int), $"{methodName} should return int");
    }

    [Theory]
    [InlineData("ShowInventoryAndSelect", typeof(Item))]
    [InlineData("ShowEquipMenuAndSelect", typeof(Item))]
    [InlineData("ShowUseMenuAndSelect", typeof(Item))]
    [InlineData("ShowCombatItemMenuAndSelect", typeof(Item))]
    public void TerminalGuiDisplayService_NullableItemReturningMethods_Exist(string methodName, Type expectedType)
    {
        // Arrange
        var type = typeof(TerminalGuiDisplayService);

        // Act
        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);

        // Assert
        method.Should().NotBeNull($"{methodName} should be implemented");
        method!.ReturnType.Should().Be(typeof(Item), $"{methodName} should return Item?");
    }

    [Fact]
    public void TerminalGuiDisplayService_ShowAbilityMenuAndSelect_Exists()
    {
        // Arrange
        var type = typeof(TerminalGuiDisplayService);

        // Act
        var method = type.GetMethod("ShowAbilityMenuAndSelect", BindingFlags.Public | BindingFlags.Instance);

        // Assert
        method.Should().NotBeNull("ShowAbilityMenuAndSelect should be implemented");
        method!.ReturnType.Should().Be(typeof(Ability), "ShowAbilityMenuAndSelect should return Ability?");
    }

    [Fact]
    public void TerminalGuiDisplayService_ShowSkillTreeMenu_Exists()
    {
        // Arrange
        var type = typeof(TerminalGuiDisplayService);

        // Act
        var method = type.GetMethod("ShowSkillTreeMenu", BindingFlags.Public | BindingFlags.Instance);

        // Assert
        method.Should().NotBeNull("ShowSkillTreeMenu should be implemented");
        method!.ReturnType.Should().Be(typeof(Skill?), "ShowSkillTreeMenu should return Skill?");
    }

    [Fact]
    public void TerminalGuiDisplayService_ShowTakeMenuAndSelect_Exists()
    {
        // Arrange
        var type = typeof(TerminalGuiDisplayService);

        // Act
        var method = type.GetMethod("ShowTakeMenuAndSelect", BindingFlags.Public | BindingFlags.Instance);

        // Assert
        method.Should().NotBeNull("ShowTakeMenuAndSelect should be implemented");
        method!.ReturnType.Should().Be(typeof(TakeSelection), "ShowTakeMenuAndSelect should return TakeSelection?");
    }

    [Fact]
    public void TerminalGuiDisplayService_ShowStartupMenu_Exists()
    {
        // Arrange
        var type = typeof(TerminalGuiDisplayService);

        // Act
        var method = type.GetMethod("ShowStartupMenu", BindingFlags.Public | BindingFlags.Instance);

        // Assert
        method.Should().NotBeNull("ShowStartupMenu should be implemented");
        method!.ReturnType.Should().Be(typeof(StartupMenuOption), "ShowStartupMenu should return StartupMenuOption");
    }

    [Fact]
    public void TerminalGuiDisplayService_SelectSaveToLoad_Exists()
    {
        // Arrange
        var type = typeof(TerminalGuiDisplayService);

        // Act
        var method = type.GetMethod("SelectSaveToLoad", BindingFlags.Public | BindingFlags.Instance);

        // Assert
        method.Should().NotBeNull("SelectSaveToLoad should be implemented");
        method!.ReturnType.Should().Be(typeof(string), "SelectSaveToLoad should return string?");
    }

    [Fact]
    public void TerminalGuiDisplayService_ReadSeed_Exists()
    {
        // Arrange
        var type = typeof(TerminalGuiDisplayService);

        // Act
        var method = type.GetMethod("ReadSeed", BindingFlags.Public | BindingFlags.Instance);

        // Assert
        method.Should().NotBeNull("ReadSeed should be implemented");
        method!.ReturnType.Should().Be(typeof(int?), "ReadSeed should return int?");
    }

    [Fact]
    public void TerminalGuiDisplayService_ShowCombatMenuAndSelect_Exists()
    {
        // Arrange
        var type = typeof(TerminalGuiDisplayService);

        // Act
        var method = type.GetMethod("ShowCombatMenuAndSelect", BindingFlags.Public | BindingFlags.Instance);

        // Assert
        method.Should().NotBeNull("ShowCombatMenuAndSelect should be implemented");
        method!.ReturnType.Should().Be(typeof(string), "ShowCombatMenuAndSelect should return string");
    }

    [Fact]
    public void TerminalGuiDisplayService_ShowConfirmMenu_Exists()
    {
        // Arrange
        var type = typeof(TerminalGuiDisplayService);

        // Act
        var method = type.GetMethod("ShowConfirmMenu", BindingFlags.Public | BindingFlags.Instance);

        // Assert
        method.Should().NotBeNull("ShowConfirmMenu should be implemented");
        method!.ReturnType.Should().Be(typeof(bool), "ShowConfirmMenu should return bool");
    }

    [Fact]
    public void TerminalGuiDisplayService_SelectClass_Exists()
    {
        // Arrange
        var type = typeof(TerminalGuiDisplayService);

        // Act
        var method = type.GetMethod("SelectClass", BindingFlags.Public | BindingFlags.Instance);

        // Assert
        method.Should().NotBeNull("SelectClass should be implemented");
        method!.ReturnType.Should().Be(typeof(PlayerClassDefinition), "SelectClass should return PlayerClassDefinition");
    }
}
