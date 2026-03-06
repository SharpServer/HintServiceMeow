using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using HintServiceMeow.Core.Enum;
using HintServiceMeow.Core.Models;
using HintServiceMeow.Core.Models.Hints;
using HintServiceMeow.Core.Utilities.Parser;

namespace HintServiceMeow.Benchmarks
{
    [Config(typeof(ScpslConfig))]
    [MemoryDiagnoser]
    public class DailyLoadBenchmark
    {
        [Params(50)]
        public int RegularHints;

        [Params(10)]
        public int DynamicHints;

        [Benchmark(Baseline = true)]
        public void ParseHintAndDynamicHints()
        {
            HintCollection collection = new HintCollection();
            HintParser parser = new HintParser();

            for (int i = 0; i < RegularHints; i++)
            {
                var staticHint = new Hint
                {
                    // Intentionally mixing valid tags with illegal tags that need to be removed by Regex 
                    // to increase the pressure on Regex processing.
                    Text = $"<color=red>Static Block {i}</color> <pos={i}>Illegal</pos> <voffset=99>Tag</voffset>",
                    XCoordinate = (i % 30) * 40 - 600, // Evenly distributed across the X-axis from -600 to 600
                    YCoordinate = (i / 30) * 45,       // Evenly distributed across the Y-axis from 0 to 900
                    FontSize = 20 + (i % 10),
                    LineHeight = 1.2f,
                    Alignment = (HintAlignment)(i % 3)
                };
                // Distribute into different Assemblies to increase the grouping overhead when traversing allGroups.
                collection.AddHint($"Assembly_Static_{i % 5}", staticHint);
            }

            for (int i = 0; i < DynamicHints; i++)
            {
                var dynamicHint = new DynamicHint
                {
                    Text = $"<size=24>Dynamic Competitor {i}</size> {{IllegalBraces}} <line-height=0>",
                    TargetX = 0,   // All attempting to crowd the center
                    TargetY = 500, // All attempting to crowd the center
                    LeftBoundary = -1000,
                    RightBoundary = 1000,
                    TopBoundary = 1000,
                    BottomBoundary = 0,
                    TopMargin = 2,
                    BottomMargin = 2,
                    LeftMargin = 5,
                    RightMargin = 5,
                    Priority = (HintPriority)(i % 5), // Mix different priorities to trigger the descending Sort logic for dynamic hints
                    Strategy = DynamicHintStrategy.StayInPosition // Force generation even if no position is found, increasing string concatenation load
                };
                collection.AddHint($"Assembly_Dynamic_{i % 3}", dynamicHint);
            }

            string resultMessage = parser.ParseToMessage(collection);

            // Ensure the result is not optimized away by the compiler 
            // (If the framework requires assertions, a rough check on resultMessage.Length can be done here).
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
                    // Intentionally mixing valid tags with illegal tags that need to be removed by Regex 
                    // to increase the pressure on Regex processing.
                    Text = $"<color=red>Static Block {i}</color> <pos={i}>Illegal</pos> <voffset=99>Tag</voffset>",
                    XCoordinate = (i % 30) * 40 - 600, // Evenly distributed across the X-axis from -600 to 600
                    YCoordinate = (i / 30) * 45,       // Evenly distributed across the Y-axis from 0 to 900
                    FontSize = 20 + (i % 10),
                    LineHeight = 1.2f,
                    Alignment = (HintAlignment)(i % 3)
                };
                // Distribute into different Assemblies to increase grouping overhead
                collection.AddHint($"Assembly_Static_{i % 5}", staticHint);
            }

            string resultMessage = parser.ParseToMessage(collection);

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
                    TargetX = 0,
                    TargetY = 500,
                    LeftBoundary = -1000,
                    RightBoundary = 1000,
                    TopBoundary = 1000,
                    BottomBoundary = 0,
                    TopMargin = 2,
                    BottomMargin = 2,
                    LeftMargin = 5,
                    RightMargin = 5,
                    Priority = (HintPriority)(i % 5), // Trigger descending Sort logic
                    Strategy = DynamicHintStrategy.StayInPosition // Increase string concatenation burden
                };
                collection.AddHint($"Assembly_Dynamic_{i % 3}", dynamicHint);
            }

            string resultMessage = parser.ParseToMessage(collection);

            if (string.IsNullOrEmpty(resultMessage))
            {
                throw new Exception("Parsed message should not be empty in this complex scenario.");
            }
        }
    }
}
