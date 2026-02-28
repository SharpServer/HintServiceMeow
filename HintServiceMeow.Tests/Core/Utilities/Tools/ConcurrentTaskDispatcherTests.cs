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
    public async Task EnqueueGeneric_ShouldReturnComputedResult()
    {
        ConcurrentTaskDispatcher dispatcher = new(1);
        int result = await dispatcher.Enqueue(() => Task.FromResult(42));
        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public async Task EnqueueGeneric_ShouldPropagateException()
    {
        ConcurrentTaskDispatcher dispatcher = new(1);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => dispatcher.Enqueue<int>(() => throw new InvalidOperationException("boom")));
    }

    [TestMethod]
    public async Task Enqueue_ShouldContinueProcessingAfterFailure()
    {
        ConcurrentTaskDispatcher dispatcher = new(1);
        TaskCompletionSource<bool> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        dispatcher.Enqueue(() => throw new InvalidOperationException("first fail"));
        Task<int> second = dispatcher.Enqueue(async () =>
        {
            tcs.SetResult(true);
            await Task.Yield();
            return 2;
        });

        await Task.WhenAny(tcs.Task, Task.Delay(1000));
        Assert.IsTrue(tcs.Task.IsCompleted);
        Assert.AreEqual(2, await second);
    }

    [TestMethod]
    public async Task Enqueue_WithConcurrentProducers_ShouldNotLoseTasks()
    {
        ConcurrentTaskDispatcher dispatcher = new(4);
        ConcurrentBag<int> bag = [];

        var tasks = Enumerable.Range(0, 100).Select(async i =>
        {
            int result = await dispatcher.Enqueue(() => Task.FromResult(i));
            bag.Add(result);
        });

        await Task.WhenAll(tasks);

        Assert.AreEqual(100, bag.Count);
    }
}
