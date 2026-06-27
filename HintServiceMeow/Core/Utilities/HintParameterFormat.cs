namespace HintServiceMeow.Core.Utilities
{
    using System.Collections.Generic;
    using System.Text;
    using HintParameter = global::Hints.HintParameter;

    internal static class HintParameterFormat
    {
        internal static bool TryReadSimplePlaceholder(string text, int startIndex, out int parameterIndex, out int endIndex)
        {
            parameterIndex = -1;
            endIndex = -1;

            if (string.IsNullOrEmpty(text) || startIndex < 0 || startIndex >= text.Length || text[startIndex] != '{')
                return false;

            int index = startIndex + 1;
            if (index >= text.Length || !char.IsDigit(text[index]))
                return false;

            int parsed = 0;
            do
            {
                int digit = text[index] - '0';
                if (parsed > (int.MaxValue - digit) / 10)
                    return false;

                parsed = (parsed * 10) + digit;

                index++;
            }
            while (index < text.Length && char.IsDigit(text[index]));

            if (index >= text.Length || text[index] != '}')
                return false;

            parameterIndex = parsed;
            endIndex = index;
            return true;
        }

        internal static void AppendRemappedText(
            string text,
            HintParameter[] parameters,
            StringBuilder destination,
            List<HintParameter> destinationParameters,
            Dictionary<int, int> indexMap)
        {
            if (parameters.Length == 0)
            {
                destination.Append(text);
                return;
            }

            for (int i = 0; i < text.Length;)
            {
                if (TryReadSimplePlaceholder(text, i, out int sourceIndex, out int endIndex) &&
                    sourceIndex >= 0 &&
                    sourceIndex < parameters.Length)
                {
                    if (!indexMap.TryGetValue(sourceIndex, out int mappedIndex))
                    {
                        mappedIndex = destinationParameters.Count;
                        indexMap[sourceIndex] = mappedIndex;
                        destinationParameters.Add(parameters[sourceIndex]);
                    }

                    destination.Append('{').Append(mappedIndex).Append('}');
                    i = endIndex + 1;
                    continue;
                }

                destination.Append(text[i]);
                i++;
            }
        }
    }
}
