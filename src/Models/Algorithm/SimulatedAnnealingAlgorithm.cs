using System;
using System.Linq;
using System.Collections.Generic;
using MCTOPP.Helpers;

namespace MCTOPP.Models.Algorithm
{
    internal enum InitialSolutionCriteria { Score, AverageDistance }
    internal enum CoolingFunction { GeometricalCooling, LundyCooling }

    internal class SelectedPoi : Poi
    {
        public bool IsSelected { get; set; } = false;

        public override string ToString()
        {
            return base.ToString() + $", IsSelected {IsSelected}";
        }
    }

    public class SimulatedAnnealingAlgorithm
    {
        int debugCount = 0;
        static Random rand = new Random();
        const int INITIAL_SOLUTION_SEED = 3;
        const int MAX_ITER_WITHOUT_IMPROVEMENT = 10;
        const double COOLING_FACTOR = 0.7;
        const int MIN_SWAP_TRIES = 1; // Per tour
        const int MAX_SWAP_TRIES = 4;
        const int MAX_RANDOM_DELETE_OPERATIONS = 12;
        const int MAX_RANDOM_INSERT_OPERATIONS = 6;
        const InitialSolutionCriteria INITIAL_SOLUTION_CRITERIA = InitialSolutionCriteria.Score;
        const CoolingFunction COOLING_FUNCTION = CoolingFunction.GeometricalCooling;

        private NLog.Logger logger = LogFactory.Create();

        public Solution Solve(ProblemInput input)
        {
            var (S, Q, P) = this.GenerateInitialSolution(input, INITIAL_SOLUTION_CRITERIA);
            var best = (Solution)S.Clone();
            logger.Debug("Initial Solution");
            logger.Debug(S.PrintSummary());

            double T = 100.0;
            int i = 0, tempIter = 0;

            while (T > 0)
            {
                debugCount++;
                var a = this.FindPoiWithHighestSpace(S, P);
                var isPivot = false;

                if (P[a.tour].ContainsKey(a.type))
                {
                    var pivot = P[a.tour][a.type].Find(p => p.IsSelected);
                    isPivot = pivot.Id == a.id;
                }

                if (INITIAL_SOLUTION_CRITERIA == InitialSolutionCriteria.AverageDistance)
                    Q = Q.OrderByDescending(p => S.MetaData.TravelAverages[p.Id]).ToList();
                else
                    Q = Q.OrderByDescending(p => p.Score).ToList();

                for (int index = 0; index < Q.Count; index++)
                {
                    if (isPivot)
                    {
                        i = MAX_ITER_WITHOUT_IMPROVEMENT + 1;
                        break;
                    }

                    var b = Q[index];

                    var R = (Solution)S.Clone();
                    var type = this.PoiTypeSelectionPolicy(b.Type, R.PoiTypeCount, a.type);

                    // Swap and validate solution
                    var swapValid = R.Swap(b.Id, type, a.pos, a.tour);
                    var solValid = swapValid ? R.IsValid : false;

                    if (swapValid && solValid)
                    {
                        var inserted = this.FillEmptySpaces(R, Q);

                        var rnd = rand.NextDouble();
                        var epsilon = Math.Pow(Math.E, (R.Score - S.Score) / T);
                        if (R.Score > S.Score || rnd < epsilon)
                        {
                            S = R;
                            Q[index] = S.MetaData.PoiIndex[a.id];
                            for (int k = inserted.Count - 1; k >= 0; k--)
                                Q.RemoveAt(inserted[k]);

                            if (isPivot)
                            {
                                // test
                            }
                        }

                        if (S.Score > best.Score)
                        {
                            best = (Solution)S.Clone();
                            // Debug(S, Q, P);
                            i = 0;
                        }
                        else
                        {
                            i++;
                        }
                        break;
                    }

                    i++;
                }

                if (i > MAX_ITER_WITHOUT_IMPROVEMENT)
                {
                    // logger.Debug("Entered swap regions");
                    // Debug(S, Q, P);

                    // logger.Debug("Started random remove:");
                    RemoveRandom(S, Q, P);
                    // Debug(S, Q, P);

                    // logger.Debug("Started pivot swap");
                    SwapPivots(S, Q, P);
                    // Debug(S, Q, P);

                    // logger.Debug("Started random insert");
                    InsertRandom(S, Q);
                    // Debug(S, Q, P);
                    // logger.Debug("Left swap regions");
                    i = 0;
                }

                if (COOLING_FUNCTION == CoolingFunction.GeometricalCooling)
                    T = this.GeometricalCoolingFunction(T, tempIter);
                else
                    T = this.LundyCoolingFunction(T);

                tempIter++;
            }

            return best;
        }

        private (Solution S, List<Poi> Q, Dictionary<int, List<SelectedPoi>>[] P) GenerateInitialSolution(
            ProblemInput input, InitialSolutionCriteria criteria
            )
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
                    if (criteria == InitialSolutionCriteria.AverageDistance)
                        candidates = candidates.OrderByDescending(p => metadata.TravelAverages[p.Id]).ToList();
                    else
                        candidates = candidates.OrderByDescending(p => p.Score).ToList();
                    pois.Add(poiType, candidates);
                    select.Add(poiType, 0);
                }
                patternPois[i] = pois;
                selected[i] = select;
            }

            // Perform pivot insertion, if can't insert switch to previous poi and move to next selected element for that poi
            var solution = new Solution(input.TourCount, metadata);
            int tourPoiPos = 0;
            for (int i = 0; i < patternPois.Length; i++)
            {
                var pattern = patternPois[i];
                var select = selected[i];
                var iter = pattern.ToList();
                for (int j = tourPoiPos; j < iter.Count; j++)
                {
                    // If reverted to beginning of the tour switch to previous tour and perform move to next selected element for that poi
                    if (j < 0)
                    {
                        i--;
                        var prev = patternPois[i].Last();
                        select = selected[i];
                        select[prev.Key]++;
                        tourPoiPos = solution.Pois[i].Count - 1;
                        solution.Remove(tourPoiPos, i);
                        i--;
                        break;
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
                        if (j - 1 >= 0)
                        {
                            var prev = iter[j - 1];
                            select[prev.Key]++;
                            solution.Remove(solution.Pois[i].Count - 1, i);
                        }
                        j -= 2;
                    }

                    if (j + 1 == iter.Count)
                        tourPoiPos = 0; // Next tour and first elem
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

            var skip = rand.Next(INITIAL_SOLUTION_SEED);
            var length = other.Count;

            for (int i = -skip; i < length; i += INITIAL_SOLUTION_SEED)
            {
                var poi = other[i + skip < length ? i + skip : length - 1];
                var spaces = solution.EmptySpaces;
                var res = false;

                var selectedTour = rand.Next(spaces.Length);
                var tour = spaces[selectedTour];

                foreach (var space in tour)
                {
                    foreach (var type in poi.Type)
                    {
                        res = solution.Insert(poi.Id, type, space.After > -1 ? space.After : solution.Pois[selectedTour].Count, selectedTour);
                        if (res) break;
                    }
                    if (res) break;
                }
            }

            var remaining = other.Where(p =>
            {
                foreach (var tour in solution.Pois)
                    foreach (var poi in tour)
                        if (poi == p.Id) return false;
                return true;
            }).ToList();

            var pivots = new Dictionary<int, List<SelectedPoi>>[input.TourCount];
            for (int i = 0; i < pivots.Length; i++)
                pivots[i] = new Dictionary<int, List<SelectedPoi>>();
            for (int i = 0; i < patternPois.Length; i++)
            {
                var pattern = patternPois[i];
                var select = selected[i];
                foreach (var item in pattern)
                {
                    pivots[i].Add(item.Key, item.Value.Select((x, j) => new SelectedPoi()
                    {
                        Id = x.Id,
                        X = x.X,
                        Y = x.Y,
                        Duration = x.Duration,
                        Score = x.Score,
                        Open = x.Open,
                        Close = x.Close,
                        Cost = x.Cost,
                        Type = x.Type,
                        IsSelected = select[item.Key] == j
                    }).ToList());
                }
            }

            return (S: solution, Q: remaining, P: pivots);
        }


        private IList<int> FillEmptySpaces(Solution solution, List<Poi> other)
        {
            var inserted = new List<int>();
            for (int i = 0; i < other.Count; i++)
            {
                var poi = other[i];
                var spaces = solution.EmptySpaces;
                var res = false;
                for (int j = 0; j < spaces.Length; j++)
                {
                    var tour = spaces[j];
                    foreach (var space in tour)
                    {
                        foreach (var type in poi.Type)
                        {
                            res = solution.Insert(poi.Id, type, space.After > -1 ? space.After : solution.Pois[j].Count, j);
                            if (res) break;
                        }
                        if (res) break;
                    }
                    if (res) break;
                }

                if (res) inserted.Add(i);
            }

            return inserted;
        }

        private (int id, int pos, int tour, int type) FindPoiWithHighestSpace(Solution S, Dictionary<int, List<SelectedPoi>>[] P)
        {
            var tour = 0;
            var pos = 0;
            var type = 0;
            var space = S.FilledSpaces[tour].ElementAt(pos);
            var pivots = new List<int>();

            foreach (var group in P)
                foreach (var item in group)
                    pivots.Add(item.Value.Find(p => p.IsSelected).Id);


            foreach (var tourSpaces in S.FilledSpaces)
            {
                foreach (var otherSpace in tourSpaces)
                {
                    if (space.Value.Size < otherSpace.Value.Size && !pivots.Contains(otherSpace.Key))
                        space = otherSpace;
                }
            }

            for (int i = 0; i < S.Pois.Length; i++)
            {
                var tourPois = S.Pois[i];
                for (int j = 0; j < tourPois.Count; j++)
                {
                    if (tourPois[j] == space.Key)
                    {
                        tour = i;
                        pos = j;
                        break;
                    }
                }
            }

            type = S.PoiTypes[space.Key];

            return (id: space.Key, pos: pos, tour: tour, type: type);
        }

        private int PoiTypeSelectionPolicy(int[] types, Dictionary<int, int> counts, int prefferedType)
        {
            if (types.Contains(prefferedType))
                return prefferedType;

            int result = types[0];
            for (int i = 1; i < types.Length; i++)
            {
                var candidate = types[i];
                if (counts[candidate] < counts[result]) result = candidate;
            }
            return result;
        }

        private double GeometricalCoolingFunction(double T, int i)
        {
            // T[i+1] = T[i] * b ** i, b closer to 1 -> slower decrease
            return Math.Round(T * Math.Pow(COOLING_FACTOR, i), 2);
        }

        private double LundyCoolingFunction(double T)
        {
            // T[i+1] = T[i] / (1 + b * T[i]), b closer to 0 -> less time in high temp
            return Math.Round(T / (1 + COOLING_FACTOR * T), 2);
        }

        private void RemoveRandom(Solution S, List<Poi> Q, Dictionary<int, List<SelectedPoi>>[] P)
        {
            var selected = new List<int>();
            foreach (var tour in P)
            {
                foreach (var item in tour)
                {
                    selected.Add(item.Value.Find(x => x.IsSelected).Id);
                }
            }

            var notPivotPositions = new List<int>[S.TourCount];
            for (int i = 0; i < S.TourCount; i++)
            {
                var list = new List<int>();
                var pois = S.Pois[i];
                for (int j = 0; j < pois.Count; j++)
                {
                    var id = pois[j];
                    if (!selected.Contains(id)) list.Add(id);
                }
                notPivotPositions[i] = list;
            }

            for (int i = 0; i < MAX_RANDOM_DELETE_OPERATIONS; i++)
            {
                var tour = rand.Next(0, S.TourCount);
                var list = notPivotPositions[tour];
                var pois = S.Pois[tour];

                if (list.Count == 0) continue;

                var index = rand.Next(0, list.Count);
                var poi = list[index];
                var pos = pois.FindIndex(x => x == poi);
                if (S.Remove(pos, tour))
                {
                    Q.Add(S.MetaData.PoiIndex[poi]);
                    list.RemoveAt(index);
                }
            }
        }

        private void InsertRandom(Solution S, List<Poi> Q)
        {
            for (int i = 0; i < MAX_RANDOM_INSERT_OPERATIONS; i++)
            {
                var index = rand.Next(0, Q.Count);
                var poi = Q[index];
                var spaces = S.EmptySpaces;

                var res = false;
                for (int j = 0; j < spaces.Length; j++)
                {
                    var tour = spaces[j];
                    foreach (var space in tour)
                    {
                        foreach (var type in poi.Type)
                        {
                            res = S.Insert(poi.Id, type, space.After > -1 ? space.After : S.Pois[j].Count, j);
                            if (res) break;
                        }
                        if (res) break;
                    }
                    if (res) break;
                }

                if (res) Q.RemoveAt(index);
            }
        }

        private void SwapPivots(Solution S, List<Poi> Q, Dictionary<int, List<SelectedPoi>>[] P)
        {
            for (int i = 0; i < S.TourCount; i++)
            {
                int pivotCount = rand.Next(MIN_SWAP_TRIES, MAX_SWAP_TRIES + 1);
                for (int r = 0; r < pivotCount; r++)
                {
                    var selectedPivotIndex = rand.Next(0, P[i].Count);
                    var selectedPivot = P[i].ElementAt(selectedPivotIndex);

                    if (selectedPivot.Value.Count <= 1)
                        continue;

                    var selectedPoiIndex = selectedPivot.Value.FindIndex(p => p.IsSelected);
                    var selectedPoi = selectedPivot.Value.ElementAt(selectedPoiIndex);
                    var replacePoiIndex = rand.Next(0, selectedPivot.Value.Count);
                    while (replacePoiIndex == selectedPoiIndex)
                        replacePoiIndex = rand.Next(0, selectedPivot.Value.Count);
                    var replacePoi = selectedPivot.Value.ElementAt(replacePoiIndex);

                    var selectedPoiIndexInS = S.Pois[i].FindIndex(p => p == selectedPoi.Id);
                    var selectedPoiTypeInS = S.PoiTypes[selectedPoi.Id];

                    var replaceTourInS = -1;
                    var replaceIndexInS = -1;
                    var replaceTypeInS = 0;
                    for (int m = 0; m < S.TourCount; m++)
                    {
                        for (int n = 0; n < S.Pois[m].Count; n++)
                        {
                            if (S.Pois[m][n] == replacePoi.Id)
                            {
                                replaceTourInS = m;
                                replaceIndexInS = n;
                                replaceTypeInS = S.PoiTypes[replacePoi.Id];
                                m = S.TourCount;
                                break;
                            }
                        }
                    }

                    if (replaceTourInS == -1 && replaceIndexInS == -1)
                    {
                        var replacePoiIndexInQ = Q.FindIndex(p => p.Id == replacePoi.Id);
                        var res = S.Swap(replacePoi.Id, selectedPivot.Key, selectedPoiIndexInS, i);
                        if (res && S.IsValid)
                        {
                            // Add old pivot to Q and remove new pivot from Q
                            Q.RemoveAt(replacePoiIndexInQ);
                            Q.Add(S.MetaData.PoiIndex[selectedPoi.Id]);
                            selectedPivot.Value[selectedPoiIndex].IsSelected = false;
                            selectedPivot.Value[replacePoiIndex].IsSelected = true;
                        }
                    }

                    // Debug(S, Q, P);
                }
            }
        }

        private void Debug(Solution S, List<Poi> Q, Dictionary<int, List<SelectedPoi>>[] P)
        {
            var strQ = "Q:" + Environment.NewLine + "[";
            for (int i = 0; i < Q.Count; i++)
                strQ += $"{Q[i].Id},";
            strQ = strQ.TrimEnd(',') + "]" + Environment.NewLine;

            var strS = "S:" + Environment.NewLine + S.PrintSummary() + Environment.NewLine;

            var strP = "P:";
            for (int i = 0; i < P.Length; i++)
            {
                strP += Environment.NewLine + $"{i} -> [";
                foreach (var item in P[i])
                    strP += "{" + $"{item.Value.Find(p => p.IsSelected).Id}->{item.Key}" + "},";
                strP = strP.TrimEnd(',') + "]";
            }
            strP += Environment.NewLine;

            var strPattern = "Pattern:";
            foreach (var item in S.MetaData.Patterns)
            {
                strPattern += Environment.NewLine + $"{item.Key} -> [";
                foreach (var _item in item.Value)
                    strPattern += $"{_item},";
                strPattern = strPattern.TrimEnd(',') + "]";
            }

            logger.Debug(
                Environment.NewLine +
                strQ + Environment.NewLine +
                strS + Environment.NewLine +
                strP + Environment.NewLine +
                strPattern
            );
        }

        public string PrintParams()
        {
            return "SA Params: " + Environment.NewLine +
                "INITIAL_SOLUTION_SEED: " + INITIAL_SOLUTION_SEED + Environment.NewLine +
                "INITIAL_SOLUTION_CRITERIA: " + INITIAL_SOLUTION_CRITERIA + Environment.NewLine +
                "MAX_ITER_WITHOUT_IMPROVEMENT: " + MAX_ITER_WITHOUT_IMPROVEMENT + Environment.NewLine +
                "COOLING_FUNCTION: " + COOLING_FUNCTION + Environment.NewLine +
                "COOLING_FACTOR: " + COOLING_FACTOR + Environment.NewLine +
                "MIN_SWAP_TRIES: " + MIN_SWAP_TRIES + Environment.NewLine +
                "MAX_SWAP_TRIES: " + MAX_SWAP_TRIES + Environment.NewLine +
                "MAX_RANDOM_DELETE_OPERATIONS: " + MAX_RANDOM_DELETE_OPERATIONS + Environment.NewLine +
                "MAX_RANDOM_INSERT_OPERATIONS: " + MAX_RANDOM_INSERT_OPERATIONS;
        }
    }
}