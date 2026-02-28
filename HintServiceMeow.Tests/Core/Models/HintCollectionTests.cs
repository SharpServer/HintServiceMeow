using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using HintServiceMeow.Core.Models;
using HintServiceMeow.Core.Models.Hints;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HintServiceMeow.Tests.Core.Models;

[TestClass]
public class HintCollectionTests
{
    [TestMethod]
    public void AddHint_ShouldRaiseAddEventAndUpdateState_When_NewHintAdded()
    {
        HintCollection collection = new();
        Hint hint = new() { Id = "A" };
        NotifyCollectionChangedEventArgs? evt = null;
        collection.CollectionChanged += (_, e) => evt = e;

        collection.AddHint("asm", hint);

        Assert.IsNotNull(evt);
        Assert.AreEqual(NotifyCollectionChangedAction.Add, evt.Action);
        CollectionAssert.Contains(collection.GetHints("asm").ToList(), hint);
    }

    [TestMethod]
    public void RemoveHint_ShouldNotRaiseEvent_When_HintDoesNotExist()
    {
        HintCollection collection = new();
        int eventCount = 0;
        collection.CollectionChanged += (_, _) => eventCount++;

        bool removed = collection.RemoveHint("asm", new Hint());

        Assert.IsFalse(removed);
        Assert.AreEqual(0, eventCount);
    }

    [TestMethod]
    public void AllHints_ShouldUseSnapshotSemantics_AcrossMutations()
    {
        HintCollection collection = new();
        Hint first = new() { Id = "1" };
        Hint second = new() { Id = "2" };

        collection.AddHint("asm", first);
        IReadOnlyList<AbstractHint> snapshot = collection.AllHints;
        collection.AddHint("asm", second);

        Assert.AreEqual(1, snapshot.Count);
        Assert.AreEqual(2, collection.AllHints.Count);
    }

    [TestMethod]
    public void RemoveHintByPredicate_ShouldCleanupEmptyGroup_When_AllRemoved()
    {
        HintCollection collection = new();
        collection.AddHint("asm", new Hint { Id = "x" });

        List<AbstractHint> removed = collection.RemoveHint("asm", _ => true);

        Assert.AreEqual(1, removed.Count);
        Assert.AreEqual(0, collection.GetHints("asm").Count);
        Assert.AreEqual(0, collection.AllGroups.Count);
    }

    [TestMethod]
    public void ConcurrentAddAndRead_ShouldStayConsistent()
    {
        HintCollection collection = new();
        ConcurrentBag<Exception> errors = [];

        Parallel.For(0, 200, i =>
        {
            try
            {
                collection.AddHint(i % 2 == 0 ? "a" : "b", new Hint { Id = i.ToString() });
                _ = collection.AllHints.Count;
                _ = collection.AllGroups.Count;
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }
        });

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(200, collection.AllHints.Count);
    }
}
