using BenchmarkDotNet.Attributes;
using SimdLinq;
using Test.KinoJoin;

namespace TestPlaywright
{
    public class SimdVsEnumerableBenchmark
    {
        private List<int> _joinEvents;

        [GlobalSetup]
        public void Setup()
        {
            var dataGenerator = new DataGenerator();
            _joinEvents = dataGenerator
                .JoinEventGenerator.Generate(1000)
                .Select(s => s.Id)
                .ToList();
        }

        [Benchmark]
        public void EnumerableMinMax()
        {
            var max = Enumerable.Max(_joinEvents);
            var min = Enumerable.Min(_joinEvents);
        }

        [Benchmark]
        public void SimdMinMax()
        {
            var (min, max) = SimdLinqExtensions.MinMax(_joinEvents);
        }

        [Benchmark]
        public void EnumerableContains()
        {
            var contains = _joinEvents.Contains(_joinEvents[0]);
        }

        [Benchmark]
        public void SimdContains()
        {
            var contains = SimdLinqExtensions.Contains(_joinEvents, _joinEvents[0]);
        }
    }
}
