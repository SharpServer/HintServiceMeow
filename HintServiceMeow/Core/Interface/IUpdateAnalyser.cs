namespace HintServiceMeow.Core.Interface
{
    using System;

    public interface IUpdateAnalyser
    {
        void OnUpdate();

        DateTime EstimateNextUpdate();
    }
}