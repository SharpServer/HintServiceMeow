using HintServiceMeow.Core.Enum;
using HintServiceMeow.Core.Models.Hints;
using HintServiceMeow.Tests.Core.Utilities.TestDoubles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HintServiceMeow.Tests.Core.Models;

[TestClass]
public class DynamicHintTests
{
    [TestMethod]
    public void Constructor_ShouldHaveExpectedDefaults()
    {
        DynamicHint hint = new();

        Assert.AreEqual(0f, hint.TopBoundary);
        Assert.AreEqual(1000f, hint.BottomBoundary);
        Assert.AreEqual(-1200f, hint.LeftBoundary);
        Assert.AreEqual(1200f, hint.RightBoundary);
        Assert.AreEqual(DynamicHintStrategy.Hide, hint.Strategy);
    }

    [TestMethod]
    public void CopyConstructor_ShouldCloneValues_AndUseNewGuid()
    {
        DynamicHint source = new()
        {
            TargetX = 1,
            TargetY = 2,
            Priority = HintPriority.High,
            Strategy = DynamicHintStrategy.StayInPosition,
            LeftMargin = 11,
        };

        DynamicHint copy = new(source);

        Assert.AreNotEqual(source.Guid, copy.Guid);
        Assert.AreEqual(source.TargetX, copy.TargetX);
        Assert.AreEqual(source.Priority, copy.Priority);
        Assert.AreEqual(source.Strategy, copy.Strategy);
        Assert.AreEqual(source.LeftMargin, copy.LeftMargin);
    }

    [TestMethod]
    public void PropertySetters_ShouldRaiseSingleUpdate_When_ValueActuallyChanges()
    {
        DynamicHint hint = new();
        FixedUpdateAnalyser analyser = new();
        hint.UpdateAnalyser = analyser;

        hint.Strategy = DynamicHintStrategy.Hide;
        hint.Strategy = DynamicHintStrategy.StayInPosition;

        Assert.AreEqual(1, analyser.OnUpdateCallCount);
    }
}
