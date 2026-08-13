namespace HintServiceMeow.Core.Utilities.Tools
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Identity comparison for dictionaries keyed by <see cref="ReferenceHub"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="UnityEngine.Object"/> collapses every destroyed instance to "null" inside its own
    /// <c>Equals</c> and <c>==</c>, so two unrelated destroyed hubs compare equal. Lookups here mean
    /// "this exact hub", so compare the reference itself.
    /// </remarks>
    internal sealed class ReferenceHubComparer : IEqualityComparer<ReferenceHub>
    {
        internal static readonly ReferenceHubComparer Instance = new();

        public bool Equals(ReferenceHub? left, ReferenceHub? right) => ReferenceEquals(left, right);

        public int GetHashCode(ReferenceHub obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
