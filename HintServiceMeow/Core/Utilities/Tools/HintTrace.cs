namespace HintServiceMeow.Core.Utilities.Tools
{
    using System;
    using System.Text;
    using HintServiceMeow.Plugin;

    internal static class HintTrace
    {
        public static bool IsEnabled
        {
            get
            {
                try
                {
                    return Plugin.Instance?.Config?.TraceHintPipeline ?? false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public static void Log(string message)
        {
            if (!IsEnabled)
                return;

            try
            {
                Logger.Instance.Info("[HSM trace] " + message);
            }
            catch
            {
                // Diagnostics must never affect hint rendering.
            }
        }

        public static string Describe(string? content)
        {
            content ??= string.Empty;
            return $"chars={content.Length} utf8={Encoding.UTF8.GetByteCount(content)} hash={Hash(content):X16}";
        }

        private static ulong Hash(string value)
        {
            const ulong OffsetBasis = 14695981039346656037UL;
            const ulong Prime = 1099511628211UL;

            ulong hash = OffsetBasis;
            byte[] bytes = Encoding.UTF8.GetBytes(value);

            for (int i = 0; i < bytes.Length; i++)
            {
                hash ^= bytes[i];
                hash *= Prime;
            }

            return hash;
        }
    }
}
