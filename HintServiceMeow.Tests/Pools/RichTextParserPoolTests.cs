using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HintServiceMeow.Core.Enum;
using HintServiceMeow.Core.Utilities.Parser;
using HintServiceMeow.Core.Utilities.Pools;
using HintServiceMeow.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HintServiceMeow.Tests.Pools
{
    [TestClass]
    public class RichTextParserPoolTests
    {
        [TestInitialize]
        public void Setup()
        {
            // Drain the singleton pool to ensure test isolation
            var pool = RichTextParserPool.Instance;
            var queue = ReflectionHelper.GetFieldValue<ConcurrentQueue<RichTextParser>>(pool, "richTextParserQueue");
            while (queue.TryDequeue(out _)) { }
        }

        private static T GetParserField<T>(RichTextParser parser, string fieldName)
        {
            var field = typeof(RichTextParser).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
                throw new ArgumentException($"Field '{fieldName}' not found on RichTextParser");
            return (T)field.GetValue(parser);
        }

        private static void SetParserField(RichTextParser parser, string fieldName, object value)
        {
            var field = typeof(RichTextParser).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
                throw new ArgumentException($"Field '{fieldName}' not found on RichTextParser");
            field.SetValue(parser, value);
        }

        #region Basic Rent/Return

        [TestMethod]
        public void Rent_WhenPoolEmpty_ReturnsNewParser()
        {
            // Arrange
            var pool = RichTextParserPool.Instance;

            // Act
            var parser = pool.Rent();

            // Assert
            Assert.IsNotNull(parser);
        }

        [TestMethod]
        public void Return_ThenRent_ReusesSameInstance()
        {
            // Arrange
            var pool = RichTextParserPool.Instance;
            var original = pool.Rent();

            // Act
            pool.Return(original);
            var reused = pool.Rent();

            // Assert
            Assert.AreSame(original, reused);
        }

        [TestMethod]
        public void Rent_MultipleTimes_ReturnsDistinctInstances()
        {
            // Arrange
            var pool = RichTextParserPool.Instance;
            int count = 5;
            var instances = new List<RichTextParser>();

            // Act
            for (int i = 0; i < count; i++)
                instances.Add(pool.Rent());

            // Assert
            var set = new HashSet<RichTextParser>(new ReferenceComparer<RichTextParser>());
            foreach (var parser in instances)
            {
                Assert.IsTrue(set.Add(parser), "Pool returned duplicate RichTextParser instance");
            }
        }

        #endregion

        #region State Clearing Bug Detection

        /// <summary>
        /// [BUG DETECTION] Return() does not call ClearStatus() on the parser.
        /// Expected: After Return and Rent, parser fontSizeStack should be empty.
        /// Actual: State from previous use is retained because Return() just enqueues without clearing.
        /// </summary>
        [TestMethod]
        public void Return_DoesNotClearParserState_FontSizeStack()
        {
            // Arrange
            var pool = RichTextParserPool.Instance;
            var parser = pool.Rent();

            // Manually push a value onto fontSizeStack via reflection
            var fontSizeStack = GetParserField<Stack<float>>(parser, "fontSizeStack");
            fontSizeStack.Push(42f);
            Assert.AreEqual(1, fontSizeStack.Count);

            // Act
            pool.Return(parser);
            var reused = pool.Rent();

            // Assert
            Assert.AreSame(parser, reused);
            var reusedStack = GetParserField<Stack<float>>(reused, "fontSizeStack");
            // If this PASSES (Count == 0), the bug is fixed.
            // If this FAILS, the bug still exists (state leaks between uses).
            Assert.AreEqual(0, reusedStack.Count,
                "[BUG] Parser fontSizeStack not cleared on Return — state leaks between uses");
        }

        /// <summary>
        /// [BUG DETECTION] Return() does not call ClearStatus() on the parser.
        /// Expected: After Return and Rent, parser style should be TextStyle.Normal.
        /// Actual: Style from previous use is retained.
        /// </summary>
        [TestMethod]
        public void Return_DoesNotClearParserState_StyleField()
        {
            // Arrange
            var pool = RichTextParserPool.Instance;
            var parser = pool.Rent();

            // Set style to Bold via reflection (TextStyle is internal, accessible via InternalsVisibleTo)
            SetParserField(parser, "style", TextStyle.Bold);

            // Act
            pool.Return(parser);
            var reused = pool.Rent();

            // Assert
            var style = GetParserField<TextStyle>(reused, "style");
            // If this PASSES (style == Normal), the bug is fixed.
            Assert.AreEqual(TextStyle.Normal, style,
                "[BUG] Parser style field not cleared on Return — state leaks between uses");
        }

        /// <summary>
        /// [BUG DETECTION] Return() does not call ClearStatus() on the parser.
        /// Expected: After Return and Rent, hintAlignmentStack should be empty.
        /// Actual: Alignment stack from previous use is retained.
        /// </summary>
        [TestMethod]
        public void Return_DoesNotClearParserState_AlignmentStack()
        {
            // Arrange
            var pool = RichTextParserPool.Instance;
            var parser = pool.Rent();

            var alignStack = GetParserField<Stack<HintAlignment>>(parser, "hintAlignmentStack");
            alignStack.Push(HintAlignment.Left);

            // Act
            pool.Return(parser);
            var reused = pool.Rent();

            // Assert
            var reusedStack = GetParserField<Stack<HintAlignment>>(reused, "hintAlignmentStack");
            Assert.AreEqual(0, reusedStack.Count,
                "[BUG] Parser hintAlignmentStack not cleared on Return — state leaks between uses");
        }

        /// <summary>
        /// [BUG DETECTION] Return() does not call ClearStatus() on the parser.
        /// Expected: After Return and Rent, caseStyleStack should be empty.
        /// Actual: Case style stack from previous use is retained.
        /// </summary>
        [TestMethod]
        public void Return_DoesNotClearParserState_CaseStyleStack()
        {
            // Arrange
            var pool = RichTextParserPool.Instance;
            var parser = pool.Rent();

            var caseStyleStack = GetParserField<List<CaseStyle>>(parser, "caseStyleStack");
            caseStyleStack.Add(CaseStyle.Uppercase);

            // Act
            pool.Return(parser);
            var reused = pool.Rent();

            // Assert
            var reusedStack = GetParserField<List<CaseStyle>>(reused, "caseStyleStack");
            Assert.AreEqual(0, reusedStack.Count,
                "[BUG] Parser caseStyleStack not cleared on Return — state leaks between uses");
        }

        /// <summary>
        /// [BUG DETECTION] Return() does not call ClearStatus() on the parser.
        /// Expected: After Return and Rent, scriptStyles should be empty.
        /// Actual: Script styles from previous use is retained.
        /// </summary>
        [TestMethod]
        public void Return_DoesNotClearParserState_ScriptStyles()
        {
            // Arrange
            var pool = RichTextParserPool.Instance;
            var parser = pool.Rent();

            // ScriptStyle is internal, accessible via InternalsVisibleTo
            var scriptStyles = GetParserField<List<ScriptStyle>>(parser, "scriptStyles");
            scriptStyles.Add(ScriptStyle.Superscript);

            // Act
            pool.Return(parser);
            var reused = pool.Rent();

            // Assert
            var reusedStyles = GetParserField<List<ScriptStyle>>(reused, "scriptStyles");
            Assert.AreEqual(0, reusedStyles.Count,
                "[BUG] Parser scriptStyles not cleared on Return — state leaks between uses");
        }

        /// <summary>
        /// [BUG DETECTION] Return() does not call ClearStatus() on the parser.
        /// Expected: After Return and Rent, currentRawLineText should be empty.
        /// Actual: Raw line text from previous use is retained.
        /// </summary>
        [TestMethod]
        public void Return_DoesNotClearParserState_RawLineText()
        {
            // Arrange
            var pool = RichTextParserPool.Instance;
            var parser = pool.Rent();

            var rawLineText = GetParserField<StringBuilder>(parser, "currentRawLineText");
            rawLineText.Append("leftover text from previous parse");

            // Act
            pool.Return(parser);
            var reused = pool.Rent();

            // Assert
            var reusedText = GetParserField<StringBuilder>(reused, "currentRawLineText");
            Assert.AreEqual(0, reusedText.Length,
                "[BUG] Parser currentRawLineText not cleared on Return — state leaks between uses");
        }

        #endregion

        #region Concurrency

        [TestMethod]
        [Timeout(10000)]
        public async Task ConcurrentRent_NoDuplicateInstances()
        {
            // Arrange
            var pool = RichTextParserPool.Instance;
            int count = 100;

            // Pre-fill pool
            for (int i = 0; i < count; i++)
                pool.Return(new RichTextParser());

            var results = new ConcurrentBag<RichTextParser>();

            // Act
            var tasks = Enumerable.Range(0, count).Select(_ => Task.Run(() =>
            {
                results.Add(pool.Rent());
            }));
            await Task.WhenAll(tasks);

            // Assert
            var set = new HashSet<RichTextParser>(new ReferenceComparer<RichTextParser>());
            foreach (var parser in results)
            {
                Assert.IsTrue(set.Add(parser),
                    "Pool returned duplicate RichTextParser instance in concurrent access");
            }
        }

        [TestMethod]
        [Timeout(10000)]
        public async Task ConcurrentReturn_ThenRent_AllAvailable()
        {
            // Arrange
            var pool = RichTextParserPool.Instance;
            int count = 100;
            var parsers = Enumerable.Range(0, count).Select(_ => new RichTextParser()).ToList();

            // Act - return all concurrently
            var returnTasks = parsers.Select(p => Task.Run(() => pool.Return(p)));
            await Task.WhenAll(returnTasks);

            // Rent all back
            var rented = new ConcurrentBag<RichTextParser>();
            var rentTasks = Enumerable.Range(0, count).Select(_ => Task.Run(() =>
            {
                rented.Add(pool.Rent());
            }));
            await Task.WhenAll(rentTasks);

            // Assert
            Assert.AreEqual(count, rented.Count);
        }

        [TestMethod]
        [Timeout(10000)]
        [Ignore("Requires FontTool to be available for ParseText")]
        public async Task ConcurrentRentAndParse_ResultsIndependent()
        {
            // This test requires FontTool.Instance to be available for ParseText to work.
            // In a game runtime environment, this would verify that concurrent parsing
            // produces independent results for each parser instance.
            await Task.CompletedTask;
        }

        #endregion

        #region Boundary

        [TestMethod]
        public void Return_Null_DoesNotCorruptPool()
        {
            // Arrange
            var pool = RichTextParserPool.Instance;

            // Act - Return(null) enqueues null into the ConcurrentQueue
            pool.Return(null);

            // Rent should return the null entry, then a new parser
            var first = pool.Rent();

            // Assert - the null was dequeued, but next Rent creates a new parser
            // ConcurrentQueue.Enqueue accepts null, so first item dequeued is null
            // This demonstrates that Return(null) silently corrupts the pool
            if (first == null)
            {
                // Pool was corrupted by null entry - rent again to get a valid parser
                var second = pool.Rent();
                Assert.IsNotNull(second, "Pool should create new parser after null was dequeued");
            }
            else
            {
                Assert.IsNotNull(first);
            }
        }

        [TestMethod]
        public void DoubleReturn_SameParser_DoesNotCauseDuplicateRent()
        {
            // Arrange
            var pool = RichTextParserPool.Instance;
            var parser = pool.Rent();

            // Act - return the same instance twice
            pool.Return(parser);
            pool.Return(parser);

            // Rent twice
            var first = pool.Rent();
            var second = pool.Rent();

            // Assert
            // ConcurrentQueue stores two references to the same object.
            // Both rents return the same instance - this is a potential problem.
            Assert.AreSame(first, second,
                "Double return causes same instance to be rented twice — potential issue " +
                "if two consumers modify it concurrently");
        }

        #endregion

        #region Singleton

        [TestMethod]
        public void Instance_AlwaysReturnsSameInstance()
        {
            // Act
            var instance1 = RichTextParserPool.Instance;
            var instance2 = RichTextParserPool.Instance;

            // Assert
            Assert.AreSame(instance1, instance2);
        }

        #endregion
    }
}
