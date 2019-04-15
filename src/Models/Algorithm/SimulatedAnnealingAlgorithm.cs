using System;
using System.Linq;
using System.Collections.Generic;
using MCTOPP.Helpers;

namespace MCTOPP.Models.Algorithm
{
    public class SimulatedAnnealingAlgorithm
    {
        protected NLog.Logger logger = LogFactory.Create();

        protected Solution GenerateInitialSolution(ProblemInput input)
        {
            var metadata = MetaData.Create(input);
            var patternPois = new Dictionary<int, List<Poi>>[input.TourCount];
            var selected = new Dictionary<int, int>[input.TourCount];

            // Find pois
            for (int i = 0; i < input.Patterns.Count; i++)
            {
                var pois = new Dictionary<int, List<Poi>>();
                var select = new Dictionary<int, int>();

                foreach (var poiType in input.Patterns[i])
                {
                    var candidates = input.Pois.FindAll(p => p.Type.Any(t => t == poiType));
                    candidates.Sort((a, b) => a.Score > b.Score ? -1 : 1); // desc
                    pois.Add(poiType, candidates);
                    select.Add(poiType, 0);
                }
                patternPois[i] = pois;
                selected[i] = select;
            }

            // Perform pivot insertion, if can't insert switch to previous poi and move to next selected element for that poi
            var solution = new Solution(input.TourCount, metadata);
            for (int i = 0; i < patternPois.Length; i++)
            {
                var pattern = patternPois[i];
                var select = selected[i];
                var iter = pattern.ToList();
                for (int j = 0; j < iter.Count; j++)
                {
                    // If reverted to beginning of the tour switch to previous tour and perform move to next selected element for that poi
                    if (j < 0)
                    {
                        i--;
                        var prev = patternPois[i].Last();
                        select[prev.Key]++;
                        solution.Remove(solution.Pois[i].Count - 1, i);
                        i--;
                    }

                    var pivot = iter[j];
                    var found = false;
                    for (int k = select[pivot.Key]; k < pivot.Value.Count; k++)
                    {
                        var poi = pivot.Value[k];
                        found = solution.Insert(poi.Id, pivot.Key, solution.Pois[i].Count, i);
                        if (found)
                        {
                            select[pivot.Key] = k;
                            break;
                        };
                    }

                    // If not found switch to previous poi and move to next selected element for that poi
                    if (!found)
                    {
                        select[pivot.Key] = 0;
                        var prev = iter[j - 1];
                        select[prev.Key]++;
                        solution.Remove(solution.Pois[i].Count - 1, i);
                        j -= 2;
                    }
                }
            }

            // Pivot insertion complete, try to fill empty spaces
            var remaining = input.Pois.Skip(0).Where(p =>
            {
                foreach (var tour in solution.Pois)
                    foreach (var poi in tour)
                        if (poi == p.Id) return false;
                return true;
            }).ToList();

            foreach (var poi in remaining)
            {
                var spaces = solution.EmptySpaces;
                var res = false;
                for (int i = 0; i < spaces.Length; i++)
                {
                    var tour = spaces[i];
                    foreach (var space in tour)
                    {
                        foreach (var type in poi.Type)
                        {
                            res = solution.Insert(poi.Id, type, space.After > -1 ? space.After : solution.Pois[i].Count, i);
                            if (res) break;
                        }
                        if (res) break;
                    }
                    if (res) break;
                }
            }

            return solution;
        }

        public Solution Solve(ProblemInput input)
        {
            var s = this.GenerateInitialSolution(input);
            logger.Log(NLog.LogLevel.Info, "Initial Solution");
            logger.Log(NLog.LogLevel.Info, s.PrintSummary());
            return null;
        }
    }
}