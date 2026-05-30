namespace HintServiceMeow.Core.Utilities.UnityAdaptors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Hints;

    /// <summary>
    /// Builds TextHint messages in a RueI-style chunked format.
    ///
    /// Mirror's NetworkWriter.WriteString has a ushort-sized per-string limit.
    /// Sending the whole rich text as TextHint.Text can therefore explode when
    /// Meow is used for very large HUD payloads, such as generated image text.
    ///
    /// This factory moves the actual content into StringHintParameter chunks and
    /// sends a tiny format string like "{0}{1}{2}" as TextHint.Text. The vanilla
    /// client already resolves TextHint parameters, so this stays client-vanilla
    /// compatible while bypassing the single-string limit.
    /// </summary>
    internal static class ChunkedHintMessageFactory
    {
        // Keep a safety margin below Mirror's 65534-byte string limit.
        private const int MaxParameterUtf8Bytes = 60000;

        private static readonly HintEffect[] AlwaysVisibleEffects = [new AlphaEffect(1)];

        internal static HintMessage Create(string content, float durationScalar = 99999f)
        {
            content ??= string.Empty;

            string[] chunks = SplitUtf8Safe(content, MaxParameterUtf8Bytes);
            HintParameter[] parameters = chunks
                .Select(chunk => (HintParameter)new StringHintParameter(chunk))
                .ToArray();

            string format = BuildFormatString(chunks.Length);
            return new HintMessage(new TextHint(format, parameters, AlwaysVisibleEffects, durationScalar));
        }

        private static string BuildFormatString(int count)
        {
            if (count <= 0)
                return string.Empty;

            var builder = new StringBuilder(count * 4);
            for (int i = 0; i < count; i++)
                builder.Append('{').Append(i).Append('}');

            return builder.ToString();
        }

        private static string[] SplitUtf8Safe(string value, int maxBytes)
        {
            if (value.Length == 0)
                return [string.Empty];

            var chunks = new List<string>();
            var builder = new StringBuilder();
            int currentBytes = 0;

            for (int i = 0; i < value.Length; i++)
            {
                string unit;
                if (char.IsHighSurrogate(value[i]) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                {
                    unit = value.Substring(i, 2);
                    i++;
                }
                else
                {
                    unit = value[i].ToString();
                }

                int unitBytes = Encoding.UTF8.GetByteCount(unit);

                if (builder.Length > 0 && currentBytes + unitBytes > maxBytes)
                {
                    chunks.Add(builder.ToString());
                    builder.Clear();
                    currentBytes = 0;
                }

                builder.Append(unit);
                currentBytes += unitBytes;
            }

            if (builder.Length > 0)
                chunks.Add(builder.ToString());

            return chunks.ToArray();
        }
    }
}
