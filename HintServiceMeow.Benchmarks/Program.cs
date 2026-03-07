using System;
using BenchmarkDotNet.Running;
using HintServiceMeow.Benchmarks;
using HintServiceMeow.Core.Utilities.Tools;

namespace MyProject.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger.Instance = new TestLogger();

            // Start the Benchmark Runner
            _ = BenchmarkRunner.Run<HintParserBenchmark>();

            Console.WriteLine("Benchmarks complete. Press any key to exit...");
            Console.ReadKey();
        }
    }
}