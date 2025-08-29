namespace HintServiceMeow.Core.Interface
{
    using System;

    internal interface IPlayerContext : IEquatable<IPlayerContext>
    {
        /// <summary>
        /// Return if the player is still valid(i.e. not disconnected).
        /// </summary>
        /// <returns>A bool :).</returns>
        bool IsValid();
    }
}
