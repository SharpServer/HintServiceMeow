using System;
using HintServiceMeow.Core.Models.Arguments;
using HintServiceMeow.Core.Models.HintContent;
using HintServiceMeow.Core.Utilities.Tools;
using HintServiceMeow.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HintServiceMeow.Tests.Core.Models.HintContent
{
    [TestClass]
    public class StringContentTests
    {
        private MockLogger _mockLogger;

        [TestInitialize]
        public void Setup()
        {
            _mockLogger = new MockLogger();
            Logger.Instance = _mockLogger;
        }

        #region Constructor

        [TestMethod]
        public void Constructor_NormalString_GetTextReturnsValue()
        {
            // Arrange & Act
            var content = new StringContent("hello");

            // Assert
            Assert.AreEqual("hello", content.GetText());
        }

        [TestMethod]
        public void Constructor_Null_GetTextReturnsNull()
        {
            // Arrange & Act
            var content = new StringContent(null);

            // Assert
            Assert.IsNull(content.GetText());
        }

        [TestMethod]
        public void Constructor_EmptyString_GetTextReturnsEmpty()
        {
            // Arrange & Act
            var content = new StringContent(string.Empty);

            // Assert
            Assert.AreEqual(string.Empty, content.GetText());
        }

        [TestMethod]
        public void Constructor_LongString_StoredCorrectly()
        {
            // Arrange
            string longString = new string('A', 10000);

            // Act
            var content = new StringContent(longString);

            // Assert
            Assert.AreEqual(longString, content.GetText());
            Assert.AreEqual(10000, content.GetText().Length);
        }

        [TestMethod]
        public void Constructor_SpecialCharacters_StoredCorrectly()
        {
            // Arrange
            string special = "Unicode: \u00E9\u00FC\u00F1 | Emoji: \uD83D\uDE00 | XML: <>&\"' | Newlines: \n\r\n\t";

            // Act
            var content = new StringContent(special);

            // Assert
            Assert.AreEqual(special, content.GetText());
        }

        #endregion

        #region Text Setter and Change Detection

        [TestMethod]
        public void SetText_DifferentValue_GetTextReturnsNewValue()
        {
            // Arrange
            var content = new StringContent("old");

            // Act
            content.Text = "new";

            // Assert
            Assert.AreEqual("new", content.GetText());
        }

        [TestMethod]
        public void SetText_DifferentValue_TriggersContentUpdated()
        {
            // Arrange
            var content = new StringContent("old");
            int eventCount = 0;
            content.ContentUpdated += () => eventCount++;

            // Act
            content.Text = "new";

            // Assert
            Assert.AreEqual(1, eventCount);
        }

        [TestMethod]
        public void SetText_SameValue_DoesNotTriggerContentUpdated()
        {
            // Arrange
            var content = new StringContent("same");
            int eventCount = 0;
            content.ContentUpdated += () => eventCount++;

            // Act
            content.Text = "same";

            // Assert
            Assert.AreEqual(0, eventCount);
        }

        [TestMethod]
        public void SetText_ToNull_TriggersContentUpdated()
        {
            // Arrange
            var content = new StringContent("hello");
            int eventCount = 0;
            content.ContentUpdated += () => eventCount++;

            // Act
            content.Text = null;

            // Assert
            Assert.AreEqual(1, eventCount);
            Assert.IsNull(content.GetText());
        }

        [TestMethod]
        public void SetText_FromNullToNonNull_TriggersContentUpdated()
        {
            // Arrange
            var content = new StringContent(null);
            int eventCount = 0;
            content.ContentUpdated += () => eventCount++;

            // Act
            content.Text = "hello";

            // Assert
            Assert.AreEqual(1, eventCount);
            Assert.AreEqual("hello", content.GetText());
        }

        [TestMethod]
        public void SetText_NullToNull_DoesNotTriggerContentUpdated()
        {
            // Arrange
            var content = new StringContent(null);
            int eventCount = 0;
            content.ContentUpdated += () => eventCount++;

            // Act
            content.Text = null;

            // Assert
            Assert.AreEqual(0, eventCount);
        }

        [TestMethod]
        public void SetText_EmptyToNull_TriggersContentUpdated()
        {
            // Arrange
            var content = new StringContent(string.Empty);
            int eventCount = 0;
            content.ContentUpdated += () => eventCount++;

            // Act
            content.Text = null;

            // Assert
            Assert.AreEqual(1, eventCount, "\"\" != null, event should fire");
        }

        #endregion

        #region Constructor and Event Interaction

        [TestMethod]
        public void Constructor_NonEmptyString_TriggersOnUpdated()
        {
            // The internal field starts as "", and the constructor sets Text = "hello".
            // Since "" != "hello", the setter runs OnUpdated().
            // We verify this indirectly: after construction, setting the same value
            // should NOT trigger the event (proving constructor already set it).

            // Act
            var content = new StringContent("hello");
            int eventCount = 0;
            content.ContentUpdated += () => eventCount++;

            // Setting same value should not trigger because constructor already set it
            content.Text = "hello";

            // Assert
            Assert.AreEqual(0, eventCount, "Setting the same value should not trigger event, proving constructor already set it via setter");
            Assert.AreEqual("hello", content.GetText());
        }

        [TestMethod]
        public void Constructor_EmptyString_DoesNotTriggerOnUpdated()
        {
            // The internal field starts as "", constructor sets Text = "".
            // Since "" == "", the setter returns early without calling OnUpdated().

            // Act
            var content = new StringContent(string.Empty);

            // Assert - text is correctly set to empty
            Assert.AreEqual(string.Empty, content.GetText());

            // No errors should have been logged
            Assert.AreEqual(0, _mockLogger.ErrorMessages.Count);
        }

        #endregion

        #region TryUpdate

        [TestMethod]
        public void TryUpdate_DoesNotModifyText()
        {
            // Arrange
            var content = new StringContent("original");
            var arg = new ContentUpdateArg(null, null);

            // Act
            content.TryUpdate(arg);

            // Assert
            Assert.AreEqual("original", content.GetText());
        }

        [TestMethod]
        public void TryUpdate_NullArg_DoesNotThrow()
        {
            // Arrange
            var content = new StringContent("test");

            // Act & Assert - should not throw since TryUpdate body is empty
            content.TryUpdate(null);
        }

        #endregion

        #region GetText and Text Consistency

        [TestMethod]
        public void GetText_AlwaysEqualsTextProperty()
        {
            // Arrange
            var content = new StringContent("initial");

            // Act & Assert - verify multiple sets
            string[] values = { "first", "second", null, "", "third" };
            foreach (var value in values)
            {
                content.Text = value;
                Assert.AreEqual(content.Text, content.GetText(),
                    $"GetText() should equal Text property after setting to '{value ?? "null"}'");
            }
        }

        [TestMethod]
        public void SetText_MultipleTimes_GetTextReflectsFinalValue()
        {
            // Arrange
            var content = new StringContent("initial");

            // Act
            content.Text = "one";
            content.Text = "two";
            content.Text = "three";

            // Assert
            Assert.AreEqual("three", content.GetText());
        }

        #endregion

        #region Event Exception Isolation

        [TestMethod]
        public void ContentUpdated_HandlerThrows_TextStillUpdated()
        {
            // Arrange
            var content = new StringContent("initial");
            content.ContentUpdated += () => throw new InvalidOperationException("handler error");

            // Act
            content.Text = "new value";

            // Assert - text should still be updated despite handler exception
            Assert.AreEqual("new value", content.GetText());
            // Exception should be logged
            Assert.AreEqual(1, _mockLogger.ErrorMessages.Count);
        }

        [TestMethod]
        public void ContentUpdated_HandlerThrows_SubsequentSetStillFiresEvent()
        {
            // Arrange
            var content = new StringContent("initial");
            AbstractHintContent.UpdateHandler throwingHandler = () => throw new InvalidOperationException("error");
            content.ContentUpdated += throwingHandler;

            // Act - first set triggers exception
            content.Text = "value1";
            Assert.AreEqual(1, _mockLogger.ErrorMessages.Count);

            // Remove throwing handler, add counting handler
            content.ContentUpdated -= throwingHandler;
            int eventCount = 0;
            content.ContentUpdated += () => eventCount++;

            // Act - second set should still fire event
            content.Text = "value2";

            // Assert
            Assert.AreEqual(1, eventCount, "Event should fire after previous handler exception");
            Assert.AreEqual("value2", content.GetText());
        }

        #endregion

        #region Polymorphism

        [TestMethod]
        public void AsAbstractHintContent_GetTextBehavesCorrectly()
        {
            // Arrange
            AbstractHintContent content = new StringContent("polymorphic");

            // Act & Assert
            Assert.AreEqual("polymorphic", content.GetText());
        }

        [TestMethod]
        public void AsAbstractHintContent_TryUpdateBehavesCorrectly()
        {
            // Arrange
            AbstractHintContent content = new StringContent("test");
            var arg = new ContentUpdateArg(null, null);

            // Act
            content.TryUpdate(arg);

            // Assert - TryUpdate is empty for StringContent, text should remain
            Assert.AreEqual("test", content.GetText());
        }

        #endregion
    }
}
