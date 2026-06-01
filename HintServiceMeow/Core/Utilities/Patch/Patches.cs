namespace HintServiceMeow.Core.Utilities.Patch
{
    using System;
    using Hints;
    using LabApi.Features.Wrappers;
    using Logger = HintServiceMeow.Core.Utilities.Tools.Logger;

    internal static class Patches
    {
#pragma warning disable SA1313
        public static bool HintDisplayPrefix(Hint hint, HintDisplay __instance)
        {
            try
            {
                return !HintCompatibilityPipeline.TryAbsorbHintDisplay(hint, __instance);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
                return true;
            }
        }

        public static void HintDisplayPostfix(Hint hint, HintDisplay __instance, bool __runOriginal)
        {
            try
            {
                HintCompatibilityPipeline.AfterOriginalHint(hint, __instance, __runOriginal);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }

        public static bool SendHintPatch1(ref string text, ref float duration, ref Player __instance)
        {
            try
            {
                return !HintCompatibilityPipeline.TryAbsorbWrapperHint(__instance?.ReferenceHub, "LabApi.SendHint(string)", text, null, duration);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
                return true;
            }
        }

#pragma warning disable IDE0060 // Remove unused parameter
        public static bool SendHintPatch2(ref string text, ref HintEffect[] effects, ref float duration, ref Player __instance)
        {
            try
            {
                return !HintCompatibilityPipeline.TryAbsorbWrapperHint(__instance?.ReferenceHub, "LabApi.SendHint(effects)", text, null, duration);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
                return true;
            }
        }

        public static bool SendHintPatch3(ref string text, ref HintParameter[] parameters, ref HintEffect[] effects, ref float duration, ref Player __instance)
        {
            try
            {
                return !HintCompatibilityPipeline.TryAbsorbWrapperHint(__instance?.ReferenceHub, "LabApi.SendHint(parameters)", text, parameters, duration);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
                return true;
            }
        }
#pragma warning restore IDE0060 // Remove unused parameter

#if EXILED
        public static bool ExiledHintPatch1(ref string message, ref float duration, ref Exiled.API.Features.Player __instance)
        {
            try
            {
                return !HintCompatibilityPipeline.TryAbsorbWrapperHint(__instance?.ReferenceHub, "Exiled.ShowHint(string)", message, null, duration);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
                return true;
            }
        }

#pragma warning disable IDE0060 // Remove unused parameter
        public static bool ExiledHintPatch2(ref string message, ref HintEffect[] hintEffects, ref float duration, ref Exiled.API.Features.Player __instance)
        {
            try
            {
                return !HintCompatibilityPipeline.TryAbsorbWrapperHint(__instance?.ReferenceHub, "Exiled.ShowHint(effects)", message, null, duration);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
                return true;
            }
        }

        public static bool ExiledHintPatch3(ref string message, ref HintParameter[] hintParameters, ref HintEffect[] hintEffects, ref float duration, ref Exiled.API.Features.Player __instance)
        {
            try
            {
                return !HintCompatibilityPipeline.TryAbsorbWrapperHint(__instance?.ReferenceHub, "Exiled.ShowHint(parameters)", message, hintParameters, duration);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
                return true;
            }
        }
#pragma warning restore IDE0060 // Remove unused parameter

        public static bool ExiledHintPatch4(ref Exiled.API.Features.Hint hint, ref Exiled.API.Features.Player __instance)
        {
            try
            {
                if (hint is null)
                {
                    HintCompatibilityPipeline.Trace("Exiled.ShowHint(Hint)", "pass-null", null, null, null);
                    return true;
                }

                if (!hint.Show)
                {
                    HintCompatibilityPipeline.Trace("Exiled.ShowHint(Hint)", "pass-hidden", null, null, hint.Content);
                    return true;
                }

                return !HintCompatibilityPipeline.TryAbsorbWrapperHint(__instance?.ReferenceHub, "Exiled.ShowHint(Hint)", hint.Content, null, hint.Duration);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
                return true;
            }
        }
#endif
#pragma warning restore SA1313
    }
}
