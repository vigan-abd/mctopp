using System;
using System.Linq;
using System.Collections.Generic;
using CommandLine;
using MCTOPP.Models.Sys;
using MCTOPP.Helpers;
using MCTOPP.Helpers.Parsers;
using MCTOPP.Models.Algorithm;

namespace MCTOPP
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<CliArgs>(args)
            .WithParsed<CliArgs>(o =>
            {
                var delimiter = Environment.OSVersion.Platform == PlatformID.Unix ||
                    Environment.OSVersion.Platform == PlatformID.MacOSX ? '/' : '\\';
                string path =
                    $"{System.IO.Directory.GetCurrentDirectory()}{delimiter}{o.InputFile.TrimStart(delimiter)}";
                IDataSetParser parser = new FileParser();
                var input = parser.ParseInput(path);
                var meta = FillMetaData(input);

                Console.WriteLine(meta.TravelTimes[0][2] == meta.TravelTimes[2][0]);
            });
        }

        static MetaData FillMetaData(ProblemInput input)
        {

            var costs = new Dictionary<int, float>();
            var durations = new Dictionary<int, float>();
            var travelTimes = new Dictionary<int, Dictionary<int, float>>();
            var hours = new Dictionary<int, (float From, float To)>();
            var poiTypes = new Dictionary<int, int>();
            for (int i = 0; i < input.Pois.Count; i++)
            {
                var poi = input.Pois[i];
                if (i > 0)
                {
                    costs.Add(poi.Id, poi.Cost);
                    durations.Add(poi.Id, poi.Duration);
                    hours.Add(poi.Id, (From: poi.Open, To: poi.Close));
                    poiTypes.Add(poi.Id, poi.Type);
                }

                var travels = new Dictionary<int, float>();
                for (int j = 0; j < input.Pois.Count; j++)
                {
                    var other = input.Pois[j];
                    travels.Add(other.Id, MathExtension.Euclidean(poi.X, poi.Y, other.X, other.Y));
                }
                travelTimes.Add(poi.Id, travels);
            }

            var travelAverages = new Dictionary<int, float>();
            foreach (var kv in travelTimes)
            {
                travelAverages.Add(kv.Key, kv.Value.Sum(x => x.Value) / kv.Value.Count);
            }

            var patterns = new Dictionary<int, int[]>();
            for (int i = 0; i < input.Patterns.Count; i++)
            {
                patterns.Add(i, input.Patterns[i]);
            }

            var maxPoisOfType = new Dictionary<int, int>();
            for (int i = 0; i < input.MaxPoisOfType.Count; i++)
            {
                maxPoisOfType.Add(i + 1, input.MaxPoisOfType[i]);
            }

            return new MetaData()
            {
                CostBudget = input.Budget,
                Costs = costs,
                TimeBudget = input.Pois[0].Close - input.Pois[0].Open,
                Durations = durations,
                PoiWorkingHours = hours,
                TravelTimes = travelTimes,
                TravelAverages = travelAverages,
                PoiTypes = poiTypes,
                MaxPoisOfType = maxPoisOfType,
                Patterns = patterns,
            };
        }
    }
}
