using System.Collections.Generic;
using HintServiceMeow.Core.Interface;

namespace HintServiceMeow.Tests.Helpers
{
    public class MockLogger : ILogger
    {
        public List<object> InfoMessages { get; } = new List<object>();

        public List<object> ErrorMessages { get; } = new List<object>();

        public List<object> DebugMessages { get; } = new List<object>();

        public void Info(object message) => InfoMessages.Add(message);

        public void Error(object message) => ErrorMessages.Add(message);

        public void Debug(object message) => DebugMessages.Add(message);

        public void Reset()
        {
            InfoMessages.Clear();
            ErrorMessages.Clear();
            DebugMessages.Clear();
        }
    }
}
