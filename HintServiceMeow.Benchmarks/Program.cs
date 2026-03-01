using System;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace MyProject.Benchmarks
{
    // 1. Custom Configuration Class: Defines which environments BenchmarkDotNet should run in
    public class MyMonoConfig : ManualConfig
    {
        public MyMonoConfig()
        {
            // Run once using the default Windows .NET Framework 4.8
            AddJob(Job.Default.WithRuntime(ClrRuntime.Net48));

            // Run again using the Mono runtime (requires "mono" to be configured in your Windows environment variables)
            // If the environment variable is not set, you can manually specify the path using the line below:
            string unityMonoPath = Environment.GetEnvironmentVariable("UNITY_MONO_PATH");
            if (unityMonoPath is null)
                throw new InvalidOperationException("Please set environment variable UNITY_MONO_PATH to the path of your Unity editor's mono.exe");

            var unityMonoJob = Job.Default
                .WithRuntime(new MonoRuntime("Unity_6000_Mono", unityMonoPath))
                .WithId("Unity 6000.0.43f1");

            AddJob(unityMonoJob);
        }
    }

    // 2. Main Benchmark Class
    [Config(typeof(MyMonoConfig))] // Apply the environment configuration defined above
    [MemoryDiagnoser]              // Enable memory allocation and GC monitoring (highly recommended)
    public class StringBenchmark
    {
        // The [Params] attribute is very powerful; it injects the values below into the test 
        // to evaluate performance across different data scales.
        [Params(100, 1000)]
        public int N;

        // [Benchmark] marks this as a method to be tested
        // Baseline = true sets this as the "reference point"; other methods will calculate a "Ratio" against it.
        [Benchmark(Baseline = true)]
        public string UseStringConcat()
        {
            string result = "";
            for (int i = 0; i < N; i++)
            {
                result += i.ToString(); // Classic inefficient string concatenation
            }
            return result;
        }

        [Benchmark]
        public string UseStringBuilder()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < N; i++)
            {
                sb.Append(i.ToString()); // Efficient StringBuilder approach
            }
            return sb.ToString();
        }
    }

    // 3. Program Entry Point
    class Program
    {
        static void Main(string[] args)
        {
            // Start the Benchmark Runner
            var summary = BenchmarkRunner.Run<StringBenchmark>();

            Console.WriteLine("Benchmarks complete. Press any key to exit...");
            Console.ReadKey();
        }
    }
}