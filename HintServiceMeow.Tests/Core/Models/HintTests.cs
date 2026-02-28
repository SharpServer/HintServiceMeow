using System.Collections.Generic;
using HintServiceMeow.Core.Enum;
using HintServiceMeow.Core.Models.HintContent;
using HintServiceMeow.Core.Models.Hints;
using HintServiceMeow.Tests.Core.Utilities.TestDoubles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HintServiceMeow.Tests.Core.Models;

[TestClass]
public class HintTests
{
    [TestMethod]
    public void Constructor_ShouldHaveExpectedDefaults()
    {
        Hint hint = new();

        Assert.AreEqual(0f, hint.XCoordinate);
        Assert.AreEqual(700f, hint.YCoordinate);
        Assert.AreEqual(HintAlignment.Center, hint.Alignment);
        Assert.AreEqual(HintVerticalAlign.Middle, hint.YCoordinateAlign);
        Assert.AreEqual(20, hint.FontSize);
    }

    [TestMethod]
    public void CopyConstructor_ShouldCloneMutableState_ButKeepDifferentGuid()
    {
        Hint source = new()
        {
            Id = "id",
            XCoordinate = 12,
            YCoordinate = 34,
            Alignment = HintAlignment.Left,
            YCoordinateAlign = HintVerticalAlign.Top,
            Text = "text"
        };

        Hint copy = new(source);

        Assert.AreNotEqual(source.Guid, copy.Guid);
        Assert.AreEqual(source.XCoordinate, copy.XCoordinate);
        Assert.AreEqual(source.YCoordinate, copy.YCoordinate);
        Assert.AreEqual(source.Alignment, copy.Alignment);
        Assert.AreEqual(source.Text, copy.Text);
    }

    [TestMethod]
    public void PropertySetters_ShouldNotifyAndInvokeAnalyser_When_ValueChanges()
    {
        Hint hint = new();
        FixedUpdateAnalyser analyser = new();
        hint.UpdateAnalyser = analyser;
        List<string> props = [];
        hint.PropertyChanged += (_, e) => props.Add(e.PropertyName!);

        hint.XCoordinate = 10;
        hint.YCoordinate = 20;

        CollectionAssert.Contains(props, "XCoordinate");
        CollectionAssert.Contains(props, "YCoordinate");
        Assert.AreEqual(2, analyser.OnUpdateCallCount);
    }

    [TestMethod]
    public void TextAndAutoText_ShouldSwitchContentImplementations()
    {
        Hint hint = new();
        hint.Text = "manual";
        Assert.IsInstanceOfType<StringContent>(hint.Content);

        hint.AutoText = _ => "auto";

        Assert.IsInstanceOfType<AutoContent>(hint.Content);
        Assert.IsNotNull(hint.AutoText);
    }

    [TestMethod]
    public void ContentReplacement_ShouldUnsubscribeOldAndSubscribeNew()
    {
        Hint hint = new();
        StringContent oldContent = new("a");
        StringContent newContent = new("b");
        hint.Content = oldContent;

        int changed = 0;
        hint.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == "Content")
                changed++;
        };

        hint.Content = newContent;
        oldContent.Text = "x";
        newContent.Text = "y";

        Assert.AreEqual(2, changed); // set + new content update only
    }
}
