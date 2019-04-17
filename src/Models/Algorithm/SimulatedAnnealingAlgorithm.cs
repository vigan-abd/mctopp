using System;
using System.Linq;
using System.Collections.Generic;
using MCTOPP.Helpers;

namespace MCTOPP.Models.Algorithm
{
    public class SimulatedAnnealingAlgorithm
    {
        static Random rand = new Random();
        const int maxIterWithoutImprovement = 10;
        protected NLog.Logger logger = LogFactory.Create();

        public Solution Solve(ProblemInput input)
        {
            var (s, q) = this.GenerateInitialSolution(input);
            var best = s;
            logger.Log(NLog.LogLevel.Debug, "Initial Solution");
            logger.Log(NLog.LogLevel.Debug, s.PrintSummary());

            var t = 100.0;
            var iter = 0;

            while (t > 0)
            {
                var a = this.FindPoiWithHighestSpace(s);
                for (int i = 0; i < q.Count; i++)
                {
                    var b = q[i];

                    var r = (Solution)s.Clone();
                    if (!r.Swap(b.Id, b.Type[0], a.pos, a.tour)) // TODO: Set Type decision policy
                        continue;
                    if (!r.IsValid)
                        continue;
                    // TODO: Fill Empty SPACES
                    if (!r.IsValid)
                        continue;
                    if (r.Score > s.Score || rand.NextDouble() < Math.Pow(Math.E, (r.Score - s.Score) / t))
                        s = r;

                    if (s.Score > best.Score)
                    {
                        best = s;
                        iter = 0;
                    }
                    else
                    {
                        iter++;
                    }

                    if (iter > maxIterWithoutImprovement)
                    {
                        // TODO:
                        // S, Q <~ shuffle(X, Y)
                        // S <~ fill_empty(Q)
                    }
                }

                t = this.CoolingFunction(t);
            }

            return s;
        }

        protected (Solution s, List<Poi> q) GenerateInitialSolution(ProblemInput input)
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
            var other = input.Pois.Skip(1).Where(p =>
            {
                foreach (var tour in solution.Pois)
                    foreach (var poi in tour)
                        if (poi == p.Id) return false;
                return true;
            }).ToList();

            var remaining = new List<Poi>();

            foreach (var poi in other)
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

                if (!res) remaining.Add(poi);
            }

            return (s: solution, q: remaining);
        }

        protected (int id, int pos, int tour) FindPoiWithHighestSpace(Solution s)
        {
            var space = s.FilledSpaces[0].First();
            var pos = 0;
            var tour = 0;

            int i = 0, j = 0;
            foreach (var tourSpaces in s.FilledSpaces)
            {
                j = 0;
                foreach (var otherSpace in tourSpaces)
                {
                    if (space.Value.Size < otherSpace.Value.Size)
                    {
                        space = otherSpace;
                        tour = i;
                        pos = j;
                    }
                    j++;
                }
                i++;
            }

            return (id: space.Key, pos: pos, tour: tour);
        }

        protected double CoolingFunction(double t)
        {
            // TODO: Implement cooling function
            return t - 100;
        }

    }
}