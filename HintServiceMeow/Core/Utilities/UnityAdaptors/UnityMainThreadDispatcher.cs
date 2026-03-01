namespace HintServiceMeow.Core.Utilities.UnityAdaptors
{
    using System;
    using HintServiceMeow.Core.Interface;

    internal class UnityMainThreadDispatcher : IMainThreadDispatcher
    {
        public void Dispatch(Action action)
        {
            global::MainThreadDispatcher.Dispatch(action);
        }
    }
}
