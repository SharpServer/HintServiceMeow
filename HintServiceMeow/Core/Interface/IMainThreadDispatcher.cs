namespace HintServiceMeow.Core.Interface
{
    using System;

    internal interface IMainThreadDispatcher
    {
        void Dispatch(Action action);
    }
}
