using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using HintServiceMeow.Core.Utilities.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HintServiceMeow.Tests.Core.Utilities.Tools;

[TestClass]
public class ConcurrentTaskDispatcherTests
{
    [TestMethod]
    public async Task EnqueueGeneric_WhenTaskCompletes_ReturnsComputedResult()
    {
        // Arrange
        ConcurrentTaskDispatcher dispatcher = new(1);

        // Act
        int result = await dispatcher.Enqueue(() => Task.FromResult(42));

        // Assert
        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public async Task EnqueueGeneric_WhenTaskThrows_PropagatesException()
    {
        // Arrange
        ConcurrentTaskDispatcher dispatcher = new(1);

        // Act & Assert
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            dispatcher.Enqueue<int>(() => throw new InvalidOperationException("boom")));
    }

    [TestMethod]
    public async Task Enqueue_WhenFirstTaskFails_ContinuesProcessingSubsequentTasks()
    {
        // Arrange
        ConcurrentTaskDispatcher dispatcher = new(1);
        TaskCompletionSource<bool> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        // Act
        dispatcher.Enqueue(() => throw new InvalidOperationException("first fail"));
        Task<int> second = dispatcher.Enqueue(async () =>
        {
            tcs.SetResult(true);
            await Task.Yield();
            return 2;
        });

        // Assert
        await Task.WhenAny(tcs.Task, Task.Delay(1000));
        Assert.IsTrue(tcs.Task.IsCompleted);
        Assert.AreEqual(2, await second);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Enqueue_WhenConcurrentProducers_DoesNotLoseTasks()
    {
        // Arrange
        ConcurrentTaskDispatcher dispatcher = new(4);
        ConcurrentBag<int> bag = [];

        // Act
        var tasks = Enumerable.Range(0, 100).Select(i => Task.Run(async () =>
        {
            int result = await dispatcher.Enqueue(() => Task.FromResult(i));
            bag.Add(result);
        }));
        await Task.WhenAll(tasks);

        // Assert
        Assert.AreEqual(100, bag.Count);
    }
}
