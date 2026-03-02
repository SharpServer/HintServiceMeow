using System;
using System.Collections.Generic;
using HintServiceMeow.Core.Enum;
using HintServiceMeow.Core.Models.Hints;
using HintServiceMeow.Core.Models.Parser;
using HintServiceMeow.Core.Utilities.Parser;
using HintServiceMeow.Core.Utilities.Tools;
using HintServiceMeow.Tests.Core.Utilities.TestDoubles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HintServiceMeow.Tests.Core.Utilities.Tools;

[TestClass]
public class CoordinateToolsTests
{
    [TestMethod]
    public void GetYCoordinate_ShouldBeReversible_BetweenTopAndBottom()
    {
        CoordinateTools tools = new();

        float bottom = tools.GetYCoordinate(500, 80, HintVerticalAlign.Top, HintVerticalAlign.Bottom);
        float restored = tools.GetYCoordinate(bottom, 80, HintVerticalAlign.Bottom, HintVerticalAlign.Top);

        Assert.AreEqual(500, restored);
    }

    [TestMethod]
    public void GetXCoordinateWithAlignment_ShouldApplyCanvasOffsets()
    {
        CoordinateTools tools = new();
        Hint hint = new() { Text = "A", FontSize = 20, XCoordinate = 100 };

        float center = tools.GetXCoordinateWithAlignment(hint, HintAlignment.Center);
        float left = tools.GetXCoordinateWithAlignment(hint, HintAlignment.Left);
        float right = tools.GetXCoordinateWithAlignment(hint, HintAlignment.Right);

        Assert.IsTrue(left < center);
        Assert.IsTrue(right > center);
    }

    [TestMethod]
    public void GetTextHeight_ShouldThrow_When_LineHeightNegative()
    {
        CoordinateTools tools = new();
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => tools.GetTextHeight("x", 20, -1));
    }

    [TestMethod]
    public void GetLineInfos_ShouldReturnPoolItem_When_ParsingCompletes()
    {
        RecordingPool<RichTextParser> pool = new(() => new RichTextParser());
        CoordinateTools tools = new(pool);

        IReadOnlyList<LineInfo> lines = tools.GetLineInfos("abc", 20);

        Assert.IsTrue(lines.Count > 0);
        Assert.AreEqual(1, pool.RentCount);
        Assert.AreEqual(1, pool.ReturnCount);
    }
}
