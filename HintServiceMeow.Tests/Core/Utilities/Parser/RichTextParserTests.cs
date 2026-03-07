using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HintServiceMeow.Core.Enum;
using HintServiceMeow.Core.Models.Parser;
using HintServiceMeow.Core.Utilities.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HintServiceMeow.Tests.Core.Utilities.Parser;

[TestClass]
public class RichTextParserTests
{
    [TestMethod]
    public void ParseText_WhenTextIsNull_ReturnsEmpty()
    {
        // Arrange
        RichTextParser parser = new();

        // Act
        IReadOnlyList<LineInfo> result = parser.ParseText(null);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void ParseText_WhenBrTagPresent_TreatsAsLineBreak()
    {
        // Arrange
        RichTextParser parser = new();

        // Act
        IReadOnlyList<LineInfo> lines = parser.ParseText("A<br>B", 20);

        // Assert
        Assert.AreEqual(2, lines.Count);
        Assert.AreEqual("A\n", lines[0].RawText);
        Assert.AreEqual("B", lines[1].RawText);
    }

    [TestMethod]
    public void ParseText_WhenUppercaseTagPresent_AppliesUppercaseTransform()
    {
        // Arrange
        RichTextParser parser = new();

        // Act
        IReadOnlyList<LineInfo> lines = parser.ParseText("<uppercase>a</uppercase>", 20);

        // Assert
        Assert.AreEqual('A', lines[0].Characters[0].Character);
    }

    [TestMethod]
    public void ParseText_WhenUnknownOrMalformedTagsPresent_RemainsStable()
    {
        // Arrange
        RichTextParser parser = new();

        // Act
        IReadOnlyList<LineInfo> lines = parser.ParseText("<foo><size=x>ab</bar>", 20);

        // Assert
        Assert.IsTrue(lines.Count >= 1);
        Assert.IsTrue(lines[0].Characters.Count >= 2);
    }

    [TestMethod]
    public void ParseText_WhenSameInputParsedTwice_ReturnsDetachedResults()
    {
        // Arrange
        RichTextParser parser = new();

        // Act
        IReadOnlyList<LineInfo> first = parser.ParseText("cache", 20);
        IReadOnlyList<LineInfo> second = parser.ParseText("cache", 20);

        // Assert
        Assert.AreNotSame(first, second);
        Assert.AreEqual(first[0].RawText, second[0].RawText);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseText_WhenCalledConcurrently_IsThreadSafe()
    {
        // Arrange
        RichTextParser parser = new();
        ConcurrentBag<Exception> errors = [];
        int parseCount = 0;

        // Act
        var tasks = Enumerable.Range(0, 50).Select(_ => Task.Run(() =>
        {
            try
            {
                IReadOnlyList<LineInfo> lines = parser.ParseText("<b>abc</b>", 20, HintAlignment.Left);
                if (lines.Count == 0)
                    throw new InvalidOperationException("No lines parsed");
                Interlocked.Increment(ref parseCount);
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }
        }));
        await Task.WhenAll(tasks);

        // Assert
        Assert.AreEqual(0, errors.Count);
        Assert.IsTrue(parseCount > 0);
    }
}
