using System;
using System.Linq;
using System.Collections.Generic;

namespace MCTOPP.Models.Algorithm
{
    public class ProblemInput
    {
        public int TourCount { get; set; }
        public int PointCount { get; set; }
        public float Budget { get; set; }
        public List<int> MaxPointsOfType { get; set; }
        public List<int> PatternLengths { get; set; }
        public List<List<int>> Patterns { get; set; }
        public List<Point> Points { get; set; }

        public override string ToString()
        {
            return $"TourCount: {TourCount}\n" +
            $"PointCount: {PointCount}\n" +
            $"Budget: {Budget}\n" +
            $"MaxPointsOfType: {String.Join(", ", MaxPointsOfType)}\n" +
            $"PatternLengths: {String.Join(", ", PatternLengths)}\n" +
            "Patterns: \n" + String.Join("\n", Patterns.Select(x => String.Join(", ", x))) + 
            "\nPoints: \n" + String.Join("\n", Points.Select(x => x.ToString()));
        }
    }
}