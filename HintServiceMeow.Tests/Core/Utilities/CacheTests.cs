using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HintServiceMeow.Core.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HintServiceMeow.Tests.Core.Utilities
{
    [TestClass]
    public class CacheTests
    {
        [TestMethod]
        public void Constructor_WhenMaxSizeInvalid_ThrowsArgumentOutOfRangeException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Cache<string, int>(0));
        }

        [TestMethod]
        public void Add_WhenKeyIsNull_ThrowsArgumentNullException()
        {
            // Arrange
            Cache<string, int> cache = new(5);

            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => cache.Add(null!, 1));
        }

        [TestMethod]
        public void AddAndTryGet_WhenKeyExists_ReturnsCorrectValue()
        {
            // Arrange
            Cache<string, int> cache = new(3);
            cache.Add("a", 1);
            cache.Add("b", 2);

            // Act
            bool foundA = cache.TryGet("a", out int v1);
            bool foundB = cache.TryGet("b", out int v2);
            bool foundC = cache.TryGet("c", out _);

            // Assert
            Assert.IsTrue(foundA);
            Assert.AreEqual(1, v1);
            Assert.IsTrue(foundB);
            Assert.AreEqual(2, v2);
            Assert.IsFalse(foundC);
        }

        [TestMethod]
        public void TryRemove_WhenKeyExists_RemovesAndReturnsValue()
        {
            // Arrange
            Cache<string, int> cache = new(3);
            cache.Add("a", 1);
            cache.Add("b", 2);

            // Act
            bool removedA = cache.TryRemove("a", out int val);
            bool foundAfterRemove = cache.TryGet("a", out _);
            bool removedC = cache.TryRemove("c", out _);

            // Assert
            Assert.IsTrue(removedA);
            Assert.AreEqual(1, val);
            Assert.IsFalse(foundAfterRemove);
            Assert.IsFalse(removedC);
        }

        [TestMethod]
        public void Add_WhenKeyExists_ReplacesOldValue()
        {
            // Arrange
            Cache<string, int> cache = new(3);
            cache.Add("a", 1);

            // Act
            cache.Add("a", 2);

            // Assert
            Assert.IsTrue(cache.TryGet("a", out int v));
            Assert.AreEqual(2, v);
        }

        [TestMethod]
        public void Add_WhenCapacityExceeded_EvictsLRUEntry()
        {
            // Arrange
            Cache<string, int> cache = new(2);
            cache.Add("a", 1);
            cache.Add("b", 2);

            // Act - "a" is oldest (LRU); adding "c" should evict it
            cache.Add("c", 3);

            // Assert
            Assert.IsFalse(cache.TryGet("a", out _));
            Assert.IsTrue(cache.TryGet("b", out int v2) && v2 == 2);
            Assert.IsTrue(cache.TryGet("c", out int v3) && v3 == 3);
        }

        [TestMethod]
        public void TryGet_WhenAccessed_PromotesToMRU()
        {
            // Arrange
            Cache<string, int> cache = new(2);
            cache.Add("a", 1);
            cache.Add("b", 2);

            // Act - access "a" to promote it; "b" becomes LRU; adding "c" evicts "b"
            cache.TryGet("a", out _);
            cache.Add("c", 3);

            // Assert
            Assert.IsTrue(cache.TryGet("a", out int v1) && v1 == 1);
            Assert.IsTrue(cache.TryGet("c", out int v2) && v2 == 3);
            Assert.IsFalse(cache.TryGet("b", out _));
        }

        [TestMethod]
        public void TryRemove_WhenKeyNotFound_ReturnsFalse()
        {
            // Arrange
            Cache<string, int> cache = new(2);
            cache.Add("a", 1);

            // Act
            bool result = cache.TryRemove("x", out _);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void AddAndTryGet_WithMultipleKeys_AllSucceed()
        {
            // Arrange
            Cache<int, string> cache = new(10);
            for (int i = 0; i < 10; i++)
                cache.Add(i, i.ToString());

            // Act & Assert
            for (int i = 0; i < 10; i++)
                Assert.IsTrue(cache.TryGet(i, out string s) && s == i.ToString());
        }

        [TestMethod]
        [Timeout(10000)]
        public async Task Cache_ConcurrentAccess_DoesNotThrow()
        {
            // Arrange
            Cache<int, int> cache = new(1000);
            ConcurrentBag<Exception> errors = [];
            int operationCount = 0;

            // Act
            var tasks = Enumerable.Range(0, 50).Select(_ => Task.Run(() =>
            {
                Random random = new();
                for (int i = 0; i < 200; i++)
                {
                    try
                    {
                        int key = random.Next(0, 1500);
                        cache.Add(key, key);
                        cache.TryGet(key, out _);
                        cache.TryRemove(key, out _);
                        Interlocked.Increment(ref operationCount);
                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex);
                    }
                }
            }));
            await Task.WhenAll(tasks);

            // Assert
            Assert.AreEqual(0, errors.Count);
            Assert.IsTrue(operationCount > 0);
        }
    }
}
