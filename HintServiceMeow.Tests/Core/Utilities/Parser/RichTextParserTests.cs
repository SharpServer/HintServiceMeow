using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    public void ParseText_ShouldReturnEmpty_When_TextIsNull()
    {
        RichTextParser parser = new();
        Assert.AreEqual(0, parser.ParseText(null).Count);
    }

    [TestMethod]
    public void ParseText_ShouldTreatBrAsLineBreak()
    {
        RichTextParser parser = new();

        IReadOnlyList<LineInfo> lines = parser.ParseText("A<br>B", 20);

        Assert.AreEqual(2, lines.Count);
        Assert.AreEqual("A\n", lines[0].RawText);
        Assert.AreEqual("B", lines[1].RawText);
    }

    [TestMethod]
    public void ParseText_ShouldApplyUppercaseTag()
    {
        RichTextParser parser = new();

        IReadOnlyList<LineInfo> lines = parser.ParseText("<uppercase>a</uppercase>", 20);

        Assert.AreEqual('A', lines[0].Characters[0].Character);
    }

    [TestMethod]
    public void ParseText_ShouldKeepStable_OnUnknownOrMalformedTags()
    {
        RichTextParser parser = new();

        IReadOnlyList<LineInfo> lines = parser.ParseText("<foo><size=x>ab</bar>", 20);

        Assert.IsTrue(lines.Count >= 1);
        Assert.IsTrue(lines[0].Characters.Count >= 2);
    }


    [TestMethod]
    public void ParseText_ShouldReturnDetachedResults_FromCache()
    {
        RichTextParser parser = new();

        IReadOnlyList<LineInfo> first = parser.ParseText("cache", 20);
        IReadOnlyList<LineInfo> second = parser.ParseText("cache", 20);

        Assert.AreNotSame(first, second);
        Assert.AreEqual(first[0].RawText, second[0].RawText);
    }

    [TestMethod]
    public void ParseText_ShouldBeThreadSafe_WithConcurrentCalls()
    {
        RichTextParser parser = new();
        ConcurrentBag<Exception> errors = [];

        Parallel.For(0, 100, _ =>
        {
            try
            {
                IReadOnlyList<LineInfo> lines = parser.ParseText("<b>abc</b>", 20, HintAlignment.Left);
                if (lines.Count == 0)
                    throw new InvalidOperationException("No lines parsed");
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }
        });

        Assert.AreEqual(0, errors.Count);
    }
}
