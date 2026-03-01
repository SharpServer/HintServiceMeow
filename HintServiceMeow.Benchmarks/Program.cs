using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using HintServiceMeow.Core.Enum;
using HintServiceMeow.Core.Interface;
using HintServiceMeow.Core.Models;
using HintServiceMeow.Core.Models.Hints;
using HintServiceMeow.Core.Utilities.Parser;
using HintServiceMeow.Core.Utilities.Tools;

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
        [Params(10, 100)]
        public int RegularHints;

        [Params(10, 100)]
        public int DynamicHints;

        // [Benchmark] marks this as a method to be tested
        // Baseline = true sets this as the "reference point"; other methods will calculate a "Ratio" against it.
        [Benchmark(Baseline = true)]
        public void ParseHintAndDynamicHints()
        {
            HintCollection collection = new HintCollection();
            HintParser parser = new HintParser();

            for (int i = 0; i < RegularHints; i++)
            {
                var staticHint = new Hint
                {
                    // 故意混杂合法与需要被 Regex 剔除的非法标签，增加正则处理压力
                    Text = $"<color=red>Static Block {i}</color> <pos={i}>Illegal</pos> <voffset=99>Tag</voffset>",
                    XCoordinate = (i % 30) * 40 - 600, // 均匀散布在 -600 到 600 的 X 轴上
                    YCoordinate = (i / 30) * 45,       // 均匀散布在 0 到 900 的 Y 轴上
                    FontSize = 20 + (i % 10),
                    LineHeight = 1.2f,
                    Alignment = (HintAlignment)(i % 3)
                };
                // 分散到不同的 Assembly 中，增加遍历 allGroups 时的分组处理压力
                collection.AddHint($"Assembly_Static_{i % 5}", staticHint);
            }

            for (int i = 0; i < DynamicHints; i++)
            {
                var dynamicHint = new DynamicHint
                {
                    Text = $"<size=24>Dynamic Competitor {i}</size> {{IllegalBraces}} <line-height=0>",
                    TargetX = 0,   // 全部试图挤在中心
                    TargetY = 500, // 全部试图挤在中心
                    LeftBoundary = -1000,
                    RightBoundary = 1000,
                    TopBoundary = 1000,
                    BottomBoundary = 0,
                    TopMargin = 2,
                    BottomMargin = 2,
                    LeftMargin = 5,
                    RightMargin = 5,
                    Priority = (HintPriority)(i % 5), // 混合不同优先级，触发动态提示的降序 Sort 逻辑
                    Strategy = DynamicHintStrategy.StayInPosition // 如果找不到位置也强制生成，增加后续字符串拼接的负担
                };
                collection.AddHint($"Assembly_Dynamic_{i % 3}", dynamicHint);
            }

            string resultMessage = parser.ParseToMessage(collection);

            // 确保结果不会被编译器优化掉 (如果框架需要断言，可以在这里对 resultMessage.Length 进行粗略断言)
            if (string.IsNullOrEmpty(resultMessage))
            {
                throw new Exception("Parsed message should not be empty in this complex scenario.");
            }
        }

        [Benchmark()]
        public void ParseHintsOnly()
        {
            HintCollection collection = new HintCollection();
            HintParser parser = new HintParser();

            for (int i = 0; i < RegularHints; i++)
            {
                var staticHint = new Hint
                {
                    // 故意混杂合法与需要被 Regex 剔除的非法标签，增加正则处理压力
                    Text = $"<color=red>Static Block {i}</color> <pos={i}>Illegal</pos> <voffset=99>Tag</voffset>",
                    XCoordinate = (i % 30) * 40 - 600, // 均匀散布在 -600 到 600 的 X 轴上
                    YCoordinate = (i / 30) * 45,       // 均匀散布在 0 到 900 的 Y 轴上
                    FontSize = 20 + (i % 10),
                    LineHeight = 1.2f,
                    Alignment = (HintAlignment)(i % 3)
                };
                // 分散到不同的 Assembly 中，增加遍历 allGroups 时的分组处理压力
                collection.AddHint($"Assembly_Static_{i % 5}", staticHint);
            }

            string resultMessage = parser.ParseToMessage(collection);

            // 确保结果不会被编译器优化掉 (如果框架需要断言，可以在这里对 resultMessage.Length 进行粗略断言)
            if (string.IsNullOrEmpty(resultMessage))
            {
                throw new Exception("Parsed message should not be empty in this complex scenario.");
            }
        }

        [Benchmark()]
        public void ParseDynamicHintsOnly()
        {
            HintCollection collection = new HintCollection();
            HintParser parser = new HintParser();

            for (int i = 0; i < DynamicHints; i++)
            {
                var dynamicHint = new DynamicHint
                {
                    Text = $"<size=24>Dynamic Competitor {i}</size> {{IllegalBraces}} <line-height=0>",
                    TargetX = 0,   // 全部试图挤在中心
                    TargetY = 500, // 全部试图挤在中心
                    LeftBoundary = -1000,
                    RightBoundary = 1000,
                    TopBoundary = 1000,
                    BottomBoundary = 0,
                    TopMargin = 2,
                    BottomMargin = 2,
                    LeftMargin = 5,
                    RightMargin = 5,
                    Priority = (HintPriority)(i % 5), // 混合不同优先级，触发动态提示的降序 Sort 逻辑
                    Strategy = DynamicHintStrategy.StayInPosition // 如果找不到位置也强制生成，增加后续字符串拼接的负担
                };
                collection.AddHint($"Assembly_Dynamic_{i % 3}", dynamicHint);
            }

            string resultMessage = parser.ParseToMessage(collection);

            // 确保结果不会被编译器优化掉 (如果框架需要断言，可以在这里对 resultMessage.Length 进行粗略断言)
            if (string.IsNullOrEmpty(resultMessage))
            {
                throw new Exception("Parsed message should not be empty in this complex scenario.");
            }
        }
    }

    // 3. Program Entry Point
    class Program
    {
        static void Main(string[] args)
        {
            Logger.Instance = new TestLogger();

            // Start the Benchmark Runner
            var summary = BenchmarkRunner.Run<StringBenchmark>();

            Console.WriteLine("Benchmarks complete. Press any key to exit...");
            Console.ReadKey();
        }
    }

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