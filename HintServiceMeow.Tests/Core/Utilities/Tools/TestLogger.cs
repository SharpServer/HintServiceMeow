using System;
using HintServiceMeow.Core.Interface;

namespace HintServiceMeow.Tests
{
    public class TestLogger : ILogger
    {
        public void Info(object message)
        {
            Console.Write("[TestLogger][Info]");
            Console.WriteLine(message);
        }

        public void Error(object message)
        {
            Console.Write("[TestLogger][Error]");
            Console.WriteLine(message);
        }

        public void Debug(object message)
        {
            Console.Write("[TestLogger][Debug]");
            Console.WriteLine(message);
        }
    }
}
