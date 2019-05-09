using System;
using System.Linq;
using System.Collections.Generic;
using MCTOPP.Helpers;

namespace MCTOPP.Models.Algorithm
{
    public class MetaData
    {
        public Poi StartingPoint { get; set; }
        public Dictionary<int, Poi> PoiIndex { get; set; }
        public float CostBudget { get; set; }
        public Dictionary<int, float> Costs { get; set; }
        public float StartTime { get; set; }
        public float EndTime { get; set; }
        public float TimeBudget { get => EndTime - StartTime; }
        public Dictionary<int, float> Durations { get; set; }
        public Dictionary<int, (float From, float To)> PoiWorkingHours { get; set; }
        public Dictionary<int, Dictionary<int, float>> TravelTimes { get; set; }
        public Dictionary<int, float> TravelAverages { get; set; }
        public Dictionary<int, int[]> PoiTypes { get; set; }
        public Dictionary<int, int> MaxPoisOfType { get; set; }
        public Dictionary<int, int[]> Patterns { get; set; }
        public Dictionary<int, float> Scores { get; set; }

        public static MetaData Create(ProblemInput input)
        {
            var costs = new Dictionary<int, float>();
            var durations = new Dictionary<int, float>();
            var travelTimes = new Dictionary<int, Dictionary<int, float>>();
            var hours = new Dictionary<int, (float From, float To)>();
            var poiTypes = new Dictionary<int, int[]>();
            var scores = new Dictionary<int, float>();
            var poiIndex = new Dictionary<int, Poi>();

            for (int i = 0; i < input.Pois.Count; i++)
            {
                var poi = input.Pois[i];
                if (i > 0)
                {
                    costs.Add(poi.Id, poi.Cost);
                    durations.Add(poi.Id, poi.Duration);
                    hours.Add(poi.Id, (From: poi.Open, To: poi.Close));
                    poiTypes.Add(poi.Id, poi.Type);
                    scores.Add(poi.Id, poi.Score);
                    poiIndex.Add(poi.Id, poi);
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
                Scores = scores,
                StartTime = input.Pois[0].Open,
                EndTime = input.Pois[0].Close,
                Durations = durations,
                PoiWorkingHours = hours,
                TravelTimes = travelTimes,
                TravelAverages = travelAverages,
                PoiTypes = poiTypes,
                MaxPoisOfType = maxPoisOfType,
                Patterns = patterns,
                StartingPoint = input.Pois[0],
                PoiIndex = poiIndex
            };
        }
    }
}