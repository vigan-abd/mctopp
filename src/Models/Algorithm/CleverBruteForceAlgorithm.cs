using System;
using System.Linq;
using System.Collections.Generic;
using MCTOPP.Helpers;

namespace MCTOPP.Models.Algorithm
{
    public class CleverBruteForceAlgorithm
    {
        protected NLog.Logger logger = LogFactory.Create();

        public Solution Solve(ProblemInput input)
        {
            Solution best = null;

            var metadata = MetaData.Create(input);
            var pivots = new Dictionary<string, List<Poi>>[input.TourCount];

            // Find pois
            for (int t = 0; t < input.Patterns.Count; t++)
            {
                var pois = new Dictionary<string, List<Poi>>();

                foreach (var poiType in input.Patterns[t])
                {
                    var candidates = input.Pois.Where(p => p.Type.Any(type => type == poiType)).OrderByDescending(p => p.Score).ToList();
                    pois.Add(Util.UniquePivot(poiType, t, pois.Count), candidates);
                }
                pivots[t] = pois;
            }

            var validSolutions = new List<Solution>();
            validSolutions.Add(new Solution(input.TourCount, metadata));

            for (int t = 0; t < input.TourCount; t++)
            {
                var pois = pivots[t];
                foreach (var item in pois)
                {
                    var clones = new List<Solution>();
                    foreach (var poi in item.Value)
                    {
                        foreach (var s in validSolutions)
                        {
                            var clone = (Solution)s.Clone();
                            if (clone.Insert(poi.Id, Util.ExtractPivot(item.Key), clone.Pois[t].Count, t))
                                clones.Add(clone);
                        }
                    }
                    validSolutions = clones;
                }
            }

            validSolutions = validSolutions.FindAll(x => x.IsValid);
            GC.Collect();

            var iter = 0;
            var iterCount = validSolutions.Count;

            // Find others
            best = validSolutions.Count > 0 ? (Solution)(validSolutions.First().Clone()) : null;
            foreach (var initialSolution in validSolutions)
            {
                GC.Collect();
                var others = input.Pois.Skip(1).Where(p => !initialSolution.PoiTypes.ContainsKey(p.Id)).OrderByDescending(p => p.Score).ToList();
                var solutions = new List<Solution>();
                solutions.Add((Solution)initialSolution.Clone());

                var i = 0;
                var count = others.Count;
                foreach (var poi in others)
                {
                    var clones = new List<Solution>();
                    foreach (var sol in solutions)
                    {
                        foreach (var type in poi.Type)
                        {
                            for (int t = 0; t < input.TourCount; t++)
                            {
                                foreach (var space in sol.EmptySpaces[t])
                                {
                                    var c = (Solution)sol.Clone();
                                    if (c.Insert(poi.Id, type, space.After > -1 ? space.After : sol.Pois[t].Count, t))
                                    {
                                        clones.Add(c);
                                        if (best.Score < c.Score)
                                        {
                                            best = c;
                                            logger.Info($"iter: {iter} of {iterCount}, poi: {i} of {count}, best so far: {best.PrintSummary()} Score: {best.Score}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    solutions.AddRange(clones);
                    i++;
                }

                iter++;
            }


            return best;
        }
    }
}