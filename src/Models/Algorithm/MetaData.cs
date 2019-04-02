using System;
using System.Collections.Generic;

namespace MCTOPP.Models.Algorithm
{
    public class MetaData
    {
        public Poi StartingPoint { get; set; }
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
    }
}