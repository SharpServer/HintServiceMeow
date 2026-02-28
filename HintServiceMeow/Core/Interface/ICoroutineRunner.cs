namespace HintServiceMeow.Core.Interface
{
    using System.Collections.Generic;

    internal interface ICoroutineRunner
    {
        ICoroutine StartCoroutine(IEnumerator<float> routine);
    }
}
