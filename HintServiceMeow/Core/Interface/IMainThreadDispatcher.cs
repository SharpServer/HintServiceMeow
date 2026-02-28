using System;

namespace HintServiceMeow.Core.Interface
{
    internal interface IMainThreadDispatcher
    {
        void Dispatch(Action action);
    }
}
