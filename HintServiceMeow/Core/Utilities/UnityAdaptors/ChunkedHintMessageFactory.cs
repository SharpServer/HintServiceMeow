namespace HintServiceMeow.Core.Utilities.UnityAdaptors
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using HintServiceMeow.Core.Utilities.Tools;
    using Mirror;
    using UnityEngine;
    using Utils.Networking;
    using AlphaCurveHintEffect = global::Hints.AlphaCurveHintEffect;
    using HintMessage = global::Hints.HintMessage;
    using HintParameter = global::Hints.HintParameter;
    using StringHintParameter = global::Hints.StringHintParameter;

    /// <summary>
    /// Sends TextHint messages in a RueI-style chunked format.
    ///
    /// Mirror's NetworkWriter.WriteString has a ushort-sized per-string limit.
    /// Sending the whole rich text as TextHint.Text can therefore fail when Meow
    /// is used for very large HUD payloads, such as generated image text.
    ///
    /// This factory sends regular-sized content as normal TextHint text. Only
    /// oversized content is moved into StringHintParameter chunks with a tiny
    /// format string like "{0}{1}{2}" as TextHint.Text.
    /// </summary>
    internal static class ChunkedHintMessageFactory
    {
        private const int MaxStringUtf8Bytes = ushort.MaxValue - 2;
        private const float DefaultDurationScalar = 999999f;
        private const byte TextHintMessageType = 1;
        private const byte StringHintParameterType = 0;

        private static readonly AlphaCurveHintEffect AlwaysVisibleEffect = new(AnimationCurve.Constant(0f, DefaultDurationScalar, 1f));

        internal static void Send(
            NetworkConnection connection,
            string content,
            HintParameter[]? parameters = null,
            float durationScalar = DefaultDurationScalar)
        {
            if (connection is null)
                throw new ArgumentNullException(nameof(connection));

            content ??= string.Empty;
            HintParameter[] hintParameters = NormalizeParameters(parameters);

            using NetworkWriterPooled writer = NetworkWriterPool.Get();

            writer.WriteUShort(NetworkMessageId<HintMessage>.Id);
            writer.WriteByte(TextHintMessageType);
            writer.WriteFloat(durationScalar);

            writer.WriteInt(1);
            writer.WriteHintEffect(AlwaysVisibleEffect);

            if (Encoding.UTF8.GetByteCount(content) <= MaxStringUtf8Bytes)
            {
                if (HintTrace.IsEnabled)
                    HintTrace.Log($"network text-direct duration={durationScalar:0.###} params={hintParameters.Length} {HintTrace.Describe(content)}");

                if (hintParameters.Length == 0)
                    WriteEmptyStringParameter(writer);
                else
                    writer.WriteHintParameterArray(hintParameters);

                writer.WriteString(content);
                connection.Send(writer.ToArraySegment());
                return;
            }

            ChunkedMessage chunkedMessage = BuildChunkedMessage(content, hintParameters);

            if (HintTrace.IsEnabled)
                HintTrace.Log($"network text-chunked duration={durationScalar:0.###} params={chunkedMessage.Parameters.Count} {HintTrace.Describe(content)}");

            writer.WriteHintParameterArray(chunkedMessage.Parameters);
            writer.WriteString(chunkedMessage.Format);
            connection.Send(writer.ToArraySegment());
        }

        private static HintParameter[] NormalizeParameters(HintParameter[]? parameters)
        {
            if (parameters is null || parameters.Length == 0)
                return [];

            HintParameter[] result = new HintParameter[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
                result[i] = parameters[i] ?? new StringHintParameter(string.Empty);

            return result;
        }

        private static void WriteEmptyStringParameter(NetworkWriter writer)
        {
            writer.WriteInt(1);
            writer.WriteByte(StringHintParameterType);
            writer.WriteString(string.Empty);
        }

        private static ChunkedMessage BuildChunkedMessage(string content, HintParameter[] parameters)
        {
            var formatBuilder = new StringBuilder();
            var outputParameters = new List<HintParameter>();

            int plainStart = 0;
            for (int i = 0; i < content.Length;)
            {
                if (HintParameterFormat.TryReadSimplePlaceholder(content, i, out int parameterIndex, out int endIndex) &&
                    parameterIndex >= 0 &&
                    parameterIndex < parameters.Length)
                {
                    AppendPlainTextParameters(content, plainStart, i - plainStart, formatBuilder, outputParameters);

                    formatBuilder.Append('{').Append(outputParameters.Count).Append('}');
                    outputParameters.Add(parameters[parameterIndex]);

                    i = endIndex + 1;
                    plainStart = i;
                    continue;
                }

                i++;
            }

            AppendPlainTextParameters(content, plainStart, content.Length - plainStart, formatBuilder, outputParameters);

            return new ChunkedMessage(formatBuilder.ToString(), outputParameters);
        }

        private static void AppendPlainTextParameters(
            string content,
            int startIndex,
            int length,
            StringBuilder formatBuilder,
            List<HintParameter> outputParameters)
        {
            if (length <= 0)
                return;

            string segment = content.Substring(startIndex, length);
            IReadOnlyList<string> chunks = SplitUtf8Safe(segment, MaxStringUtf8Bytes);

            foreach (string chunk in chunks)
            {
                formatBuilder.Append('{').Append(outputParameters.Count).Append('}');
                outputParameters.Add(new StringHintParameter(chunk));
            }
        }

        private static IReadOnlyList<string> SplitUtf8Safe(string value, int maxBytes)
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

        private sealed class ChunkedMessage(string format, IReadOnlyCollection<HintParameter> parameters)
        {
            public string Format { get; } = format;

            public IReadOnlyCollection<HintParameter> Parameters { get; } = parameters;
        }
    }
}
