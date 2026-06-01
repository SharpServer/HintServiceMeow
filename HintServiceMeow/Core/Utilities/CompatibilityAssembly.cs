namespace HintServiceMeow.Core.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using HintServiceMeow.Plugin;

    internal static class CompatibilityAssembly
    {
        internal const string UnknownAssemblyName = "<unknown>";

        private static readonly string ThisAssemblyName = typeof(CompatibilityAssembly).Assembly.GetName().Name ?? string.Empty;

        private static readonly HashSet<string> FrameworkAssemblyNames = new(StringComparer.OrdinalIgnoreCase)
        {
            ThisAssemblyName,
            "0Harmony",
            "HarmonyLib",
            "MonoMod.RuntimeDetour",
            "MonoMod.Utils",
            "Assembly-CSharp",
            "Assembly-CSharp-firstpass",
            "CommandSystem.Core",
            "LabApi",
            "Mirror",
            "NorthwoodLib",
            "UnityEngine.CoreModule",
            "mscorlib",
            "netstandard",
        };

        internal static bool IsDisabled(string? assemblyName)
        {
            string fullAssemblyName = assemblyName ?? string.Empty;
            if (string.IsNullOrWhiteSpace(fullAssemblyName))
                return false;

            List<string>? disabledAssemblies;
            try
            {
                disabledAssemblies = Plugin.Instance?.Config?.DisabledCompatAdapter;
            }
            catch
            {
                return false;
            }

            if (disabledAssemblies is null)
                return false;

            string simpleName = GetSimpleName(fullAssemblyName);

            foreach (string disabledAssembly in disabledAssemblies)
            {
                if (string.IsNullOrWhiteSpace(disabledAssembly))
                    continue;

                if (string.Equals(disabledAssembly, fullAssemblyName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(GetSimpleName(disabledAssembly), simpleName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        internal static bool IsFrameworkAssembly(Assembly assembly)
        {
            if (assembly is null)
                return true;

            if (assembly.IsDynamic)
                return true;

            return IsFrameworkAssemblyName(assembly.GetName().Name);
        }

        internal static bool IsFrameworkAssemblyName(string? assemblyName)
        {
            string simpleName = assemblyName ?? string.Empty;
            if (string.IsNullOrWhiteSpace(simpleName))
                return true;

            if (FrameworkAssemblyNames.Contains(simpleName))
                return true;

            return simpleName.StartsWith("System", StringComparison.Ordinal)
                   || simpleName.StartsWith("Microsoft.", StringComparison.Ordinal)
                   || simpleName.StartsWith("Exiled.", StringComparison.Ordinal)
                   || simpleName.StartsWith("UnityEngine.", StringComparison.Ordinal);
        }

        internal static string GetSimpleName(string? assemblyName)
        {
            string fullAssemblyName = assemblyName ?? string.Empty;
            if (string.IsNullOrWhiteSpace(fullAssemblyName))
                return UnknownAssemblyName;

            try
            {
                return new AssemblyName(fullAssemblyName).Name ?? fullAssemblyName;
            }
            catch
            {
                int commaIndex = fullAssemblyName.IndexOf(',');
                return commaIndex > 0 ? fullAssemblyName.Substring(0, commaIndex).Trim() : fullAssemblyName.Trim();
            }
        }
    }
}
