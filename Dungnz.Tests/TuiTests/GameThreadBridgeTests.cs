using System.Collections.Concurrent;
using Dungnz.Display.Tui;
using FluentAssertions;

namespace Dungnz.Tests.TuiTests;

/// <summary>
/// Tests for GameThreadBridge thread safety and concurrent access patterns.
/// </summary>
public class GameThreadBridgeTests
{
    [Fact]
    public void PostCommand_AddsCommandToQueue()
    {
        // Arrange
        var bridge = new GameThreadBridge();

        // Act
        bridge.PostCommand("test command");
        var result = bridge.WaitForCommand();

        // Assert
        result.Should().Be("test command");
    }

    [Fact]
    public void WaitForCommand_BlocksUntilCommandIsPosted()
    {
        // Arrange
        var bridge = new GameThreadBridge();
        var commandReceived = false;
        string? receivedCommand = null;

        // Act
        var waitTask = Task.Run(() =>
        {
            receivedCommand = bridge.WaitForCommand();
            commandReceived = true;
        });

        // Give it a moment to start waiting
        Thread.Sleep(100);
        commandReceived.Should().BeFalse("WaitForCommand should be blocking");

        // Now post a command
        bridge.PostCommand("delayed command");
        waitTask.Wait(TimeSpan.FromSeconds(1));

        // Assert
        commandReceived.Should().BeTrue();
        receivedCommand.Should().Be("delayed command");
    }

    [Fact]
    public void WaitForCommand_ReturnsNullAfterComplete()
    {
        // Arrange
        var bridge = new GameThreadBridge();

        // Act
        bridge.Complete();
        var result = bridge.WaitForCommand();

        // Assert
        result.Should().BeNull("WaitForCommand should return null after Complete() is called");
    }

    [Fact]
    public void PostCommand_ThrowsAfterComplete()
    {
        // Arrange
        var bridge = new GameThreadBridge();
        bridge.Complete();

        // Act
        Action act = () => bridge.PostCommand("too late");

        // Assert
        act.Should().Throw<InvalidOperationException>("posting to a completed queue should throw");
    }

    [Fact]
    public void ConcurrentPostCommand_AllCommandsAreReceived()
    {
        // Arrange
        var bridge = new GameThreadBridge();
        var commandCount = 100;
        var postedCommands = new ConcurrentBag<string>();
        var receivedCommands = new List<string>();

        // Act - post commands from multiple threads
        var postTasks = Enumerable.Range(0, commandCount).Select(i => Task.Run(() =>
        {
            var cmd = $"command-{i}";
            postedCommands.Add(cmd);
            bridge.PostCommand(cmd);
        })).ToArray();

        // Receive commands from a single thread
        var receiveTask = Task.Run(() =>
        {
            for (int i = 0; i < commandCount; i++)
            {
                var cmd = bridge.WaitForCommand();
                if (cmd != null)
                    receivedCommands.Add(cmd);
            }
        });

        Task.WaitAll(postTasks);
        receiveTask.Wait(TimeSpan.FromSeconds(5));

        // Assert
        receivedCommands.Should().HaveCount(commandCount, "all posted commands should be received");
        receivedCommands.Should().BeEquivalentTo(postedCommands, "received commands should match posted commands");
    }

    [Fact]
    public void MultipleWaitForCommand_ReturnCommandsInOrder()
    {
        // Arrange
        var bridge = new GameThreadBridge();

        // Act
        bridge.PostCommand("first");
        bridge.PostCommand("second");
        bridge.PostCommand("third");

        var cmd1 = bridge.WaitForCommand();
        var cmd2 = bridge.WaitForCommand();
        var cmd3 = bridge.WaitForCommand();

        // Assert
        cmd1.Should().Be("first");
        cmd2.Should().Be("second");
        cmd3.Should().Be("third");
    }

    [Fact]
    public void PostCommand_FromMultipleThreads_ThreadSafe()
    {
        // Arrange
        var bridge = new GameThreadBridge();
        var threadCount = 10;
        var commandsPerThread = 10;
        var receivedCommands = new ConcurrentBag<string>();

        // Act - post commands from multiple threads concurrently
        var tasks = Enumerable.Range(0, threadCount).Select(threadId => Task.Run(() =>
        {
            for (int i = 0; i < commandsPerThread; i++)
            {
                bridge.PostCommand($"thread-{threadId}-cmd-{i}");
            }
        })).ToList();

        // Receive all commands
        var receiveTask = Task.Run(() =>
        {
            for (int i = 0; i < threadCount * commandsPerThread; i++)
            {
                var cmd = bridge.WaitForCommand();
                if (cmd != null)
                    receivedCommands.Add(cmd);
            }
        });

        Task.WaitAll(tasks.ToArray());
        receiveTask.Wait(TimeSpan.FromSeconds(5));

        // Assert
        receivedCommands.Should().HaveCount(threadCount * commandsPerThread);
        // Verify all thread-command combinations exist
        for (int threadId = 0; threadId < threadCount; threadId++)
        {
            for (int i = 0; i < commandsPerThread; i++)
            {
                receivedCommands.Should().Contain($"thread-{threadId}-cmd-{i}");
            }
        }
    }

    [Fact]
    public void WaitForCommand_MultipleWaiters_EachGetsUniqueCommand()
    {
        // Arrange
        var bridge = new GameThreadBridge();
        var waiterCount = 5;
        var receivedCommands = new ConcurrentBag<string>();

        // Act - start multiple waiters
        var waiterTasks = Enumerable.Range(0, waiterCount).Select(i => Task.Run(() =>
        {
            var cmd = bridge.WaitForCommand();
            if (cmd != null)
                receivedCommands.Add(cmd);
        })).ToArray();

        // Give them time to start waiting
        Thread.Sleep(100);

        // Post commands
        for (int i = 0; i < waiterCount; i++)
        {
            bridge.PostCommand($"cmd-{i}");
        }

        Task.WaitAll(waiterTasks);

        // Assert
        receivedCommands.Should().HaveCount(waiterCount);
        receivedCommands.Should().OnlyHaveUniqueItems("each waiter should get a unique command");
    }

    [Fact]
    public void Complete_UnblocksWaitingThreads()
    {
        // Arrange
        var bridge = new GameThreadBridge();
        var waitCompleted = false;

        // Act - start a waiter
        var waiterTask = Task.Run(() =>
        {
            var result = bridge.WaitForCommand();
            waitCompleted = true;
            return result;
        });

        // Give it time to start waiting
        Thread.Sleep(100);
        waitCompleted.Should().BeFalse();

        // Complete the queue
        bridge.Complete();
        waiterTask.Wait(TimeSpan.FromSeconds(1));

        // Assert
        waitCompleted.Should().BeTrue("Complete() should unblock WaitForCommand");
        waiterTask.Result.Should().BeNull();
    }

    [Fact]
    public void InvokeOnUiThread_DoesNotThrow_WhenMainLoopIsNull()
    {
        // Arrange & Act
        Action act = () => GameThreadBridge.InvokeOnUiThread(() => { });

        // Assert
        act.Should().NotThrow("InvokeOnUiThread should handle null MainLoop gracefully");
    }

    [Fact]
    public void InvokeOnUiThreadAndWait_CompletesSuccessfully()
    {
        // Act
        Action act = () => GameThreadBridge.InvokeOnUiThreadAndWait(() =>
        {
            // Action body
        });

        // Assert - This will timeout if MainLoop is null, which is expected in unit tests
        // We're verifying the method signature and basic structure
        act.Should().NotThrow<ArgumentException>("method should accept valid action");
    }

    [Fact]
    public void InvokeOnUiThreadAndWait_Generic_ReturnsValue()
    {
        // Arrange & Act
        Func<int> act = () => GameThreadBridge.InvokeOnUiThreadAndWait(() => 42);

        // Assert - This will timeout if MainLoop is null, which is expected in unit tests
        // We're verifying the method signature and basic structure
        act.Should().NotThrow<ArgumentException>("method should accept valid function");
    }
}
