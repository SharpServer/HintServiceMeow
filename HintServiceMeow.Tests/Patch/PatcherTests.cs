using System.Reflection;
using HintServiceMeow.Core.Utilities.Patch;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HintServiceMeow.Tests.Patch
{
    [TestClass]
    public class PatcherTests
    {
        [TestInitialize]
        public void Setup()
        {
            // Reset Harmony property to null via reflection (private set)
            var backingField = typeof(Patcher).GetField(
                "<Harmony>k__BackingField",
                BindingFlags.Static | BindingFlags.NonPublic);
            backingField?.SetValue(null, null);
        }

        #region Patch Installation

        [TestMethod]
        [Ignore("Requires game assemblies (Hints.HintDisplay, LabApi.Features.Wrappers.Player)")]
        public void Patch_SetsHarmonyInstanceNotNull()
        {
            // Arrange & Act
            // Patcher.Patch();

            // Assert
            // Assert.IsNotNull(Patcher.Harmony);
        }

        [TestMethod]
        [Ignore("Requires game assemblies (Hints.HintDisplay, LabApi.Features.Wrappers.Player)")]
        public void Patch_HarmonyIdContainsGuid_UniquePerCall()
        {
            // Arrange & Act
            // Patcher.Patch();
            // var harmony1 = Patcher.Harmony;
            // Patcher.Patch();
            // var harmony2 = Patcher.Harmony;

            // Assert
            // Assert.AreNotEqual(harmony1.Id, harmony2.Id);
            // Assert.IsTrue(harmony1.Id.StartsWith("HintServiceMeowHarmony"));
            // Assert.IsTrue(harmony2.Id.StartsWith("HintServiceMeowHarmony"));
        }

        [TestMethod]
        [Ignore("Requires game assemblies (Hints.HintDisplay, LabApi.Features.Wrappers.Player)")]
        public void Patch_AllTargetMethodsArePatched()
        {
            // Arrange & Act
            // Patcher.Patch();

            // Assert
            // Verify that HintDisplay.Show, Player.SendHint(string,float),
            // Player.SendHint(string,HintEffect[],float) are all patched
            // var patches = Harmony.GetAllPatchedMethods();
            // Assert.IsTrue(patches.Any(m => m.Name == "Show"));
        }

        [TestMethod]
        [Ignore("Requires game assemblies (Hints.HintDisplay, LabApi.Features.Wrappers.Player)")]
        public void Patch_UnpatchesExistingPatchesFirst()
        {
            // Arrange & Act
            // Patcher.Patch(); // First patch
            // Patcher.Patch(); // Should unpatch first, then re-patch

            // Assert
            // Verify methods are still correctly patched (not double-patched)
        }

        [TestMethod]
        [Ignore("Requires game assemblies (Hints.HintDisplay, LabApi.Features.Wrappers.Player)")]
        public void Patch_WhenReflectionReturnsNull_ThrowsClearException()
        {
            // Arrange & Act & Assert
            // If the target type doesn't have the expected method,
            // GetMethod returns null and the subsequent call throws NullReferenceException
        }

        #endregion

        #region Unpatch

        [TestMethod]
        [Ignore("Requires assembly Harmony")]
        public void Unpatch_WhenHarmonyIsNull_DoesNotThrow()
        {
            // Arrange - Harmony is null (reset in Setup)
            //Assert.IsNull(Patcher.Harmony);

            // Act & Assert - should not throw
            //Patcher.Unpatch();
        }

        [TestMethod]
        [Ignore("Requires game assemblies (Hints.HintDisplay, LabApi.Features.Wrappers.Player)")]
        public void Unpatch_AfterPatch_RemovesAllPatches()
        {
            // Arrange
            // Patcher.Patch();
            // Assert.IsNotNull(Patcher.Harmony);

            // Act
            // Patcher.Unpatch();

            // Assert
            // Verify all patched methods are unpatched
        }

        [TestMethod]
        [Ignore("Requires game assemblies (Hints.HintDisplay, LabApi.Features.Wrappers.Player)")]
        public void Unpatch_DoesNotAffectOtherHarmonyIds()
        {
            // Arrange
            // var otherHarmony = new Harmony("other-id");
            // Patcher.Patch();

            // Act
            // Patcher.Unpatch();

            // Assert
            // Other harmony instance's patches should remain unaffected
        }

        #endregion

        #region Lifecycle

        [TestMethod]
        [Ignore("Requires game assemblies (Hints.HintDisplay, LabApi.Features.Wrappers.Player)")]
        public void Patch_Unpatch_Patch_IsReentrant()
        {
            // Arrange & Act
            // Patcher.Patch();
            // Patcher.Unpatch();
            // Patcher.Patch(); // Should work without issues

            // Assert
            // Assert.IsNotNull(Patcher.Harmony);
        }

        [TestMethod]
        [Ignore("Requires game assemblies (Hints.HintDisplay, LabApi.Features.Wrappers.Player)")]
        public void MultiplePatch_WithoutUnpatch_CreatesNewHarmonyEachTime()
        {
            // Arrange & Act
            // Patcher.Patch();
            // var first = Patcher.Harmony;
            // Patcher.Patch();
            // var second = Patcher.Harmony;

            // Assert
            // Assert.AreNotSame(first, second);
        }

        [TestMethod]
        [Ignore("Requires game assemblies (Hints.HintDisplay, LabApi.Features.Wrappers.Player)")]
        public void Unpatch_HarmonyPropertyRetainsReference()
        {
            // Arrange
            // Patcher.Patch();
            // var harmony = Patcher.Harmony;

            // Act
            // Patcher.Unpatch();

            // Assert
            // Harmony property should still reference the Harmony instance
            // (UnpatchAll doesn't set the property to null)
            // Assert.IsNotNull(Patcher.Harmony);
            // Assert.AreSame(harmony, Patcher.Harmony);
        }

        #endregion
    }
}
