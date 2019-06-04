using System;
using System.Linq;
using System.Collections.Generic;
using MCTOPP.Helpers;

namespace MCTOPP.Models.Algorithm
{
    public class BruteForceAlgorithm
    {
        protected NLog.Logger logger = LogFactory.Create();

        public Solution Solve(ProblemInput input)
        {
            var tourCount = input.TourCount;
            var metadata = MetaData.Create(input);
            var pois = input.Pois.Skip(1).ToArray();
            var keys = pois.Select((x, index) => index).ToArray();
            var perms = keys.Permutations(); // Key combinations

            var solutions = new List<Solution>();
            var i = 0;

            foreach (var perm in perms)
            {
                if (i % 10000 == 0)
                    logger.Debug("Iter: " + i);
                i++;

                // perm -> i-th combination
                var tourSolutions = new List<Solution>();
                tourSolutions.Add(new Solution(tourCount, metadata));

                foreach (var item in perm)
                {
                    var poi = pois[item];
                    var splitBranch = poi.Type.Length > 1;

                    var clones = new List<Solution>();

                    foreach (var variant in tourSolutions)
                    {
                        for (int t = 1; t < tourCount; t++)
                        {
                            var replica = (Solution)variant.Clone();
                            this.InsertTypes(splitBranch, t, true, poi, replica, ref clones);
                        }
                        this.InsertTypes(splitBranch, 0, false, poi, variant, ref clones);
                    }
                    if (clones.Count > 0) tourSolutions.AddRange(clones);

                    GC.Collect();
                }

                foreach (var solution in tourSolutions)
                {
                    if (solution.IsValid)
                    {
                        logger.Debug("Valid Solution");
                        logger.Debug(solution.PrintSummary() + $" Score: {solution.Score}");
                        solutions.Add(solution);
                    }
                }
            }

            var score = 0.0f;
            Solution result = null;
            foreach (var item in solutions)
            {
                if (score < item.Score)
                {
                    score = item.Score;
                    result = item;
                }
            }

            return result;
        }

        protected void InsertTypes(bool splitBranch, int tour, bool forceClone, Poi poi, Solution variant, ref List<Solution> clones)
        {
            if (splitBranch)
            {
                for (int i = 1; i < poi.Type.Length; i++)
                {
                    var replica = (Solution)variant.Clone();
                    var _res = replica.Insert(poi.Id, poi.Type[i], variant.Pois[tour].Count, tour);
                    if (_res) clones.Add(replica);
                }
            }
            var res = variant.Insert(poi.Id, poi.Type[0], variant.Pois[tour].Count, tour);
            if (forceClone && res) clones.Add(variant);
        }
    }
}