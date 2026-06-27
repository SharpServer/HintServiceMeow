namespace HintServiceMeow.Core.Models.Arguments
{
    using HintParameter = global::Hints.HintParameter;

    /// <summary>
    /// Provides data passed to an <see cref="ICompatibilityAdaptor"/> when sending a hint to a player.
    /// </summary>
    public class CompatibilityAdaptorArg
    {
        internal CompatibilityAdaptorArg(string assemblyName, string? content, HintParameter[]? parameters, float duration)
        {
            AssemblyName = assemblyName;
            Content = content;
            Parameters = parameters ?? [];
            Duration = duration;
        }

        /// <summary>
        /// Gets the name of the assembly that registered the hint.
        /// </summary>
        public string AssemblyName { get; }

        /// <summary>
        /// Gets the formatted hint content string to display, or <see langword="null"/> if there is no content.
        /// </summary>
        public string? Content { get; }

        /// <summary>
        /// Gets native SCP:SL hint parameters referenced by <see cref="Content"/>.
        /// </summary>
        public HintParameter[] Parameters { get; }

        /// <summary>
        /// Gets the duration in seconds for which the hint should be displayed.
        /// </summary>
        public float Duration { get; }
    }
}
