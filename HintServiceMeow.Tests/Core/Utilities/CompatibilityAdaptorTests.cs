using System;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using HintServiceMeow.Core.Models.Hints;
using HintServiceMeow.Core.Utilities;
using HintServiceMeow.Core.Utilities.Parser;
using HintServiceMeow.Plugin;
using HintServiceMeow.Tests.Core.Utilities.TestDoubles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HintServiceMeow.Tests.Core.Utilities;

[TestClass]
public class CompatibilityAdaptorTests
{
    [TestInitialize]
    public void Setup() => EnsurePluginConfig();

    [TestMethod]
    public void ShowHint_ShouldThrow_When_ArgIsNull()
    {
        CompatibilityAdaptor adaptor = CreateAdaptor(out _);
        Assert.ThrowsExactly<ArgumentNullException>(() => adaptor.ShowHint(null!));
    }

    [TestMethod]
    public void ShowHint_ShouldClearGroup_When_DurationIsNonPositive()
    {
        CompatibilityAdaptor adaptor = CreateAdaptor(out PlayerDisplay display);
        display.InternalAddHint("CompatibilityAdaptor-asm", new Hint { Text = "old" });

        adaptor.ShowHint(new("asm", "any", 0));

        Assert.AreEqual(0, display.InternalGetHints("CompatibilityAdaptor-asm").Count);
    }

    [TestMethod]
    public void ShowHint_ShouldIgnoreDisabledAssembly()
    {
        PluginConfig.Instance.DisabledCompatAdapter.Clear();
        PluginConfig.Instance.DisabledCompatAdapter.Add("blocked");

        CompatibilityAdaptor adaptor = CreateAdaptor(out PlayerDisplay display);
        adaptor.ShowHint(new("blocked", "content", 1));

        Thread.Sleep(50);
        Assert.AreEqual(0, display.InternalGetHints("CompatibilityAdaptor-blocked").Count);
    }

    [TestMethod]
    public void Destruct_ShouldBeIdempotent_AndPreventFurtherUpdates()
    {
        CompatibilityAdaptor adaptor = CreateAdaptor(out PlayerDisplay display);
        ((HintServiceMeow.Core.Interface.IDestructible)adaptor).Destruct();
        ((HintServiceMeow.Core.Interface.IDestructible)adaptor).Destruct();

        adaptor.ShowHint(new("asm", "later", 1f));
        Thread.Sleep(50);

        Assert.AreEqual(0, display.InternalGetHints("CompatibilityAdaptor-asm").Count);
    }

    private static CompatibilityAdaptor CreateAdaptor(out PlayerDisplay display)
    {
        TestPlayerContext context = new() { IsStillValid = true };
        display = new PlayerDisplay(
            context,
            updateScheduler: new TestTaskScheduler(),
            adaptor: new TestCompatibilityAdaptor(),
            hintParser: new TestHintParser(),
            coroutineRunner: new TestCoroutineRunner(),
            dispatcher: new TestMainThreadDispatcher());

        return new CompatibilityAdaptor(display, new RecordingPool<RichTextParser>(() => new RichTextParser()));
    }

    private static void EnsurePluginConfig()
    {
        FieldInfo instanceField = typeof(Plugin.Plugin).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic)!;
        if (instanceField.GetValue(null) is not null)
            return;

        Plugin.Plugin fakePlugin = (Plugin.Plugin)FormatterServices.GetUninitializedObject(typeof(Plugin.Plugin));
        typeof(Plugin.Plugin).GetField("<Config>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(fakePlugin, new PluginConfig { DisabledCompatAdapter = [] });
        instanceField.SetValue(null, fakePlugin);
    }
}
