using System;
using HintServiceMeow.Core.Interface;

namespace HintServiceMeow.Benchmarks
{
    class TestLogger : ILogger
    {
        public void Error(object ex)
        {
            Console.WriteLine($"[TestLogger][Error] {ex}");
        }

        public void Info(object message)
        {
            Console.WriteLine($"[TestLogger][Info] {message}");
        }

        public void Debug(object message)
        {
            Console.WriteLine($"[TestLogger][Debug] {message}");
        }
    }
}
