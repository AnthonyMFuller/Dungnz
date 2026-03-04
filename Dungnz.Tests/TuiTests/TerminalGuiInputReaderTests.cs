using Dungnz.Display.Tui;
using Dungnz.Engine;
using FluentAssertions;

namespace Dungnz.Tests.TuiTests;

/// <summary>
/// Tests for TerminalGuiInputReader verifying it correctly bridges input via GameThreadBridge.
/// </summary>
public class TerminalGuiInputReaderTests
{
    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenBridgeIsNull()
    {
        // Arrange, Act & Assert
        var act = () => new TerminalGuiInputReader(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("bridge");
    }

    [Fact]
    public void Constructor_CreatesInstance_WithValidBridge()
    {
        // Arrange
        var bridge = new GameThreadBridge();

        // Act
        var reader = new TerminalGuiInputReader(bridge);

        // Assert
        reader.Should().NotBeNull();
        reader.Should().BeAssignableTo<IInputReader>();
    }

    [Fact]
    public void ReadLine_ReturnsCommand_FromBridge()
    {
        // Arrange
        var bridge = new GameThreadBridge();
        var reader = new TerminalGuiInputReader(bridge);

        // Post a command from another thread (simulating UI thread)
        var postTask = Task.Run(() =>
        {
            Thread.Sleep(50); // Brief delay to ensure ReadLine is waiting
            bridge.PostCommand("test command");
        });

        // Act - ReadLine should block until command is posted
        var result = reader.ReadLine();

        // Assert
        postTask.Wait();
        result.Should().Be("test command");
    }

    [Fact]
    public void ReadLine_ReturnsNull_WhenBridgeIsCompleted()
    {
        // Arrange
        var bridge = new GameThreadBridge();
        var reader = new TerminalGuiInputReader(bridge);
        bridge.Complete();

        // Act
        var result = reader.ReadLine();

        // Assert
        result.Should().BeNull("ReadLine should return null when bridge is completed");
    }

    [Fact]
    public void ReadLine_HandlesMultipleCommands_InSequence()
    {
        // Arrange
        var bridge = new GameThreadBridge();
        var reader = new TerminalGuiInputReader(bridge);

        // Act
        bridge.PostCommand("first");
        bridge.PostCommand("second");
        bridge.PostCommand("third");

        var cmd1 = reader.ReadLine();
        var cmd2 = reader.ReadLine();
        var cmd3 = reader.ReadLine();

        // Assert
        cmd1.Should().Be("first");
        cmd2.Should().Be("second");
        cmd3.Should().Be("third");
    }

    [Fact]
    public void ReadKey_ReturnsNull()
    {
        // Arrange
        var bridge = new GameThreadBridge();
        var reader = new TerminalGuiInputReader(bridge);

        // Act
        var result = reader.ReadKey();

        // Assert
        result.Should().BeNull("TUI uses modal dialogs, not Console.ReadKey");
    }

    [Fact]
    public void IsInteractive_ReturnsFalse()
    {
        // Arrange
        var bridge = new GameThreadBridge();
        var reader = new TerminalGuiInputReader(bridge);

        // Act
        var result = reader.IsInteractive;

        // Assert
        result.Should().BeFalse("TUI uses modal dialogs, not interactive key reading");
    }

    [Fact]
    public void ReadLine_BlocksUntilCommandAvailable()
    {
        // Arrange
        var bridge = new GameThreadBridge();
        var reader = new TerminalGuiInputReader(bridge);
        var readStarted = false;
        var readCompleted = false;

        // Act
        var readTask = Task.Run(() =>
        {
            readStarted = true;
            var result = reader.ReadLine();
            readCompleted = true;
            return result;
        });

        // Wait for read to start
        Thread.Sleep(100);
        readStarted.Should().BeTrue();
        readCompleted.Should().BeFalse("ReadLine should be blocking");

        // Post command
        bridge.PostCommand("delayed");
        readTask.Wait(TimeSpan.FromSeconds(1));

        // Assert
        readCompleted.Should().BeTrue();
        readTask.Result.Should().Be("delayed");
    }

    [Fact]
    public void ReadLine_FromMultipleThreads_EachGetsUniqueCommand()
    {
        // Arrange
        var bridge = new GameThreadBridge();
        var reader = new TerminalGuiInputReader(bridge);
        var readerCount = 5;
        var receivedCommands = new List<string?>();
        var lockObj = new object();

        // Act - start multiple readers
        var readerTasks = Enumerable.Range(0, readerCount).Select(i => Task.Run(() =>
        {
            var cmd = reader.ReadLine();
            lock (lockObj)
            {
                receivedCommands.Add(cmd);
            }
            return cmd;
        })).ToArray();

        // Give them time to start
        Thread.Sleep(100);

        // Post commands
        for (int i = 0; i < readerCount; i++)
        {
            bridge.PostCommand($"cmd-{i}");
        }

        Task.WaitAll(readerTasks);

        // Assert
        receivedCommands.Should().HaveCount(readerCount);
        receivedCommands.Should().OnlyHaveUniqueItems();
        receivedCommands.Should().AllSatisfy(cmd => cmd.Should().NotBeNullOrEmpty());
    }

    [Fact]
    public void ReadLine_CanHandleEmptyStrings()
    {
        // Arrange
        var bridge = new GameThreadBridge();
        var reader = new TerminalGuiInputReader(bridge);

        // Act
        bridge.PostCommand("");
        var result = reader.ReadLine();

        // Assert
        result.Should().Be("");
    }

    [Fact]
    public void ReadLine_CanHandleWhitespace()
    {
        // Arrange
        var bridge = new GameThreadBridge();
        var reader = new TerminalGuiInputReader(bridge);

        // Act
        bridge.PostCommand("   ");
        var result = reader.ReadLine();

        // Assert
        result.Should().Be("   ");
    }

    [Fact]
    public void ReadLine_CanHandleSpecialCharacters()
    {
        // Arrange
        var bridge = new GameThreadBridge();
        var reader = new TerminalGuiInputReader(bridge);

        // Act
        bridge.PostCommand("!@#$%^&*()");
        var result = reader.ReadLine();

        // Assert
        result.Should().Be("!@#$%^&*()");
    }
}
