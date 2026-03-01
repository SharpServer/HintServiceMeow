namespace HintServiceMeow.Core.Utilities.UnityAdaptors
{
    using HintServiceMeow.Core.Interface;
    using MEC;

    internal class UnityCoroutine : ICoroutine
    {
        private CoroutineHandle handle;

        internal UnityCoroutine(CoroutineHandle handle)
        {
            this.handle = handle;
        }

        public bool IsRunning => handle.IsRunning;

        public bool IsPaused => handle.IsAliveAndPaused;

        public void Kill()
        {
            Timing.KillCoroutines(handle);
        }

        public void Pause()
        {
            Timing.PauseCoroutines(handle);
        }

        public void Resume()
        {
            Timing.ResumeCoroutines(handle);
        }
    }
}
