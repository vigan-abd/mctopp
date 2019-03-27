using System;
using System.Linq;
using System.Collections.Generic;

namespace MCTOPP.Models.Algorithm
{
    public class ProblemInput
    {
        public int TourCount { get; set; }
        public int PoiCount { get; set; }
        public float Budget { get; set; }
        public List<int> MaxPoisOfType { get; set; }
        public List<int> PatternLengths { get; set; }
        public List<int[]> Patterns { get; set; }
        public List<Poi> Pois { get; set; }

        public override string ToString()
        {
            return $"TourCount: {TourCount}{Environment.NewLine}" +
            $"PointCount: {PoiCount}{Environment.NewLine}" +
            $"Budget: {Budget}{Environment.NewLine}" +
            $"MaxPointsOfType: {String.Join(", ", MaxPoisOfType)}{Environment.NewLine}" +
            $"PatternLengths: {String.Join(", ", PatternLengths)}{Environment.NewLine}" +
            $"Patterns: {Environment.NewLine}" +
                String.Join(Environment.NewLine, Patterns.Select(x => String.Join(", ", x))) +
            $"{Environment.NewLine}Points: {Environment.NewLine}" +
                String.Join(Environment.NewLine, Pois.Select(x => x.ToString()));
        }
    }
}