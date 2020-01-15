using System;
using System.Collections.Generic;
using System.Linq;

namespace MCTOPP.Models.Algorithm
{
    public class FilledSpace : ICloneable
    {
        public float Start { get; set; }
        public float End { get; set; }
        public float Size { get => End - Start; }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    public class EmptySpace : FilledSpace
    {
        public int Before { get; set; } = -1;
        public int After { get; set; } = -1;
    }

    public class Solution : ICloneable
    {
        public int TourCount { get; protected set; }

        public float Cost { get; set; }

        public float[] Durations { get; set; }

        public float Score { get; set; }

        /// <summary>Poi identifiers for each tour</summary>
        public List<int>[] Pois { get; set; }

        /// <summary>Poi type of each item, {id, type} keypair</summary>
        public Dictionary<int, string> PoiTypes { get; set; }

        /// <summary>Count for each poi type inside solution</summary>
        public Dictionary<string, int> PoiTypeCount { get; set; }

        public Dictionary<int, FilledSpace>[] FilledSpaces { get; set; }

        public List<EmptySpace>[] EmptySpaces { get; set; }

        public MetaData MetaData { get; protected set; }

        public bool IsValid { get { return this.IsPatternValid() && this.ArePoisUnique(); } }

        public Solution(int tourCount, MetaData metadata)
        {
            this.MetaData = metadata;
            this.TourCount = tourCount;

            this.Pois = new List<int>[this.TourCount];
            this.PoiTypes = new Dictionary<int, string>();
            this.PoiTypeCount = new int[10].Select((x, i) => i + 1).ToDictionary(k => k.ToString(), v => 0);

            this.FilledSpaces = new Dictionary<int, FilledSpace>[this.TourCount];
            this.EmptySpaces = new List<EmptySpace>[this.TourCount];

            this.Durations = new float[this.TourCount];

            for (int i = 0; i < this.TourCount; i++)
            {
                this.Pois[i] = new List<int>();
                this.FilledSpaces[i] = new Dictionary<int, FilledSpace>();
                this.EmptySpaces[i] = new List<EmptySpace>()
                {
                    new EmptySpace
                    {
                        Start = this.MetaData.StartTime,
                        End = this.MetaData.EndTime
                    }
                };
            }
        }

        public bool Insert(int id, string type, int pos, int tour)
        {
            var pois = this.Pois[tour];
            var filledSpaces = this.FilledSpaces[tour];

            // Check if id exists
            if (this.PoiTypes.ContainsKey(id))
                return false;

            // Check if position is inside range
            if (pos > pois.Count)
                return false;

            // Check if insertion violates max allowed poi type
            if (this.PoiTypeCount[type] + 1 > this.MetaData.MaxPoisOfType[type])
                return false;

            // Check if insertion violates budget
            var poiCost = this.MetaData.Costs[id];
            if (this.Cost + poiCost > this.MetaData.CostBudget)
                return false;

            // Check if insertion violates open or close hours for any item
            FilledSpace space = null;
            if (pos > 0)
            {
                var prevPoi = pois[pos - 1]; // id
                var prevSpace = filledSpaces[prevPoi];
                space = this.CalculateFilledSpace(id, prevSpace, prevPoi);
            }
            else
            {
                space = this.CalculateFilledSpace(id);
            }

            if (space == null)
                return false;

            // Calculate movements of pois that are after curr poi
            var movements = new Dictionary<int, FilledSpace>();
            int moveIndex = -1;
            for (int i = pos; i < pois.Count; i++)
            {
                var poi = pois[i]; //id
                var nextSpace = filledSpaces[poi];

                var prevPoi = moveIndex >= 0 ? pois[i - 1] : id;
                var prevSpace = moveIndex >= 0 ? movements[prevPoi] : space;

                FilledSpace newSpace = this.CalculateFilledSpace(poi, prevSpace, prevPoi);

                if (newSpace == null)
                    return false;

                // break if no movement in current space
                if (newSpace.Start == nextSpace.Start && newSpace.End == nextSpace.End)
                    break;

                moveIndex++;
                movements[poi] = newSpace;
            }

            // Check if exceeds return time to point 0
            if (movements.Count > 0 && moveIndex == pois.Count - 1)
            {
                var last = movements.Last();
                var travelTime = this.MetaData.TravelTimes[last.Key][0];
                if (last.Value.End + travelTime > this.MetaData.TimeBudget)
                    return false;
            }
            else if (pos == pois.Count)
            {
                var last = space;
                var travelTime = this.MetaData.TravelTimes[id][0];
                if (last.End + travelTime > this.MetaData.TimeBudget)
                    return false;
            }
            else
            {
                var lastId = pois.Last();
                var last = filledSpaces[lastId];
                var travelTime = this.MetaData.TravelTimes[lastId][0];
                if (last.End + travelTime > this.MetaData.TimeBudget)
                    return false;
            }

            //Safe change state
            foreach (var kv in movements)
            {
                filledSpaces[kv.Key] = kv.Value;
            }

            this.PoiTypes[id] = type;
            this.PoiTypeCount[type]++;
            this.Cost += poiCost;
            this.Score += this.MetaData.Scores[id];
            filledSpaces.Add(id, space);
            pois.Insert(pos, id);
            this.Durations[tour] = filledSpaces[pois.Last()].End - this.MetaData.StartTime;
            this.CalculateEmptySpaces(tour);

            return true;
        }

        public bool Remove(int pos, int tour)
        {
            var pois = this.Pois[tour];
            var filledSpaces = this.FilledSpaces[tour];

            // Check if position is inside range
            if (pos >= pois.Count || pos < 0)
                return false;

            var movements = new Dictionary<int, FilledSpace>();
            var moveIndex = -1;
            for (int i = pos + 1; i < pois.Count; i++)
            {
                var poi = pois[i]; //id
                var nextSpace = filledSpaces[poi];

                FilledSpace newSpace = null;
                if (i == 1)
                {
                    newSpace = this.CalculateFilledSpace(poi);
                }
                else
                {
                    var prevPoi = moveIndex >= 0 ? pois[i - 1] : pois[pos - 1];
                    var prevSpace = moveIndex >= 0 ? movements[prevPoi] : filledSpaces[prevPoi];
                    newSpace = this.CalculateFilledSpace(poi, prevSpace, prevPoi);
                }

                if (newSpace == null)
                    return false;

                moveIndex++;
                movements[poi] = newSpace;
            }

            // Check if exceeds return time to point 0
            if (pos == pois.Count - 1 && pos != 0)
            {
                var lastId = pois[pois.Count - 2];
                var last = filledSpaces[lastId];
                if (moveIndex == pois.Count - 2)
                {
                    var _last = movements.Last();
                    lastId = _last.Key;
                    last = _last.Value;
                }
                var travelTime = this.MetaData.TravelTimes[lastId][0];
                if (last.End + travelTime > this.MetaData.TimeBudget)
                    return false;
            }
            else if (movements.Count > 0 && moveIndex == pois.Count - 1)
            {
                var last = movements.Last();
                var travelTime = this.MetaData.TravelTimes[last.Key][0];
                if (last.Value.End + travelTime > this.MetaData.TimeBudget)
                    return false;
            }
            else
            {
                var lastId = pois.Last();
                var last = filledSpaces[lastId];
                var travelTime = this.MetaData.TravelTimes[lastId][0];
                if (last.End + travelTime > this.MetaData.TimeBudget)
                    return false;
            }

            //Safe change state
            foreach (var kv in movements)
            {
                filledSpaces[kv.Key] = kv.Value;
            }

            var deleteId = pois[pos];
            var deletePoiCost = this.MetaData.Costs[deleteId];
            var deletePoiType = this.PoiTypes[deleteId];

            this.PoiTypeCount[deletePoiType]--;
            this.Cost -= deletePoiCost;
            this.Score -= this.MetaData.Scores[deleteId];
            filledSpaces.Remove(deleteId);
            this.PoiTypes.Remove(deleteId);
            pois.RemoveAt(pos);
            this.Durations[tour] = filledSpaces.Count > 0 ? filledSpaces[pois.Last()].End - this.MetaData.StartTime : 0;
            this.CalculateEmptySpaces(tour);
            return true;
        }

        public bool Swap(int id, string type, int pos, int tour)
        {
            var pois = this.Pois[tour];
            var filledSpaces = this.FilledSpaces[tour];

            // Check if id exists
            if (this.PoiTypes.ContainsKey(id))
                return false;

            // Check if position is inside range
            if (pos >= pois.Count)
                return false;

            if (pos == -1)
                return false;

            var oldPoi = pois[pos];
            var oldPoiType = this.PoiTypes[oldPoi];

            // Check if insertion violates max allowed poi type
            if (oldPoiType != type && this.PoiTypeCount[type] + 1 > this.MetaData.MaxPoisOfType[type])
                return false;

            // Check if insertion violates budget
            var oldPoiCost = this.MetaData.Costs[oldPoi];
            var poiCost = this.MetaData.Costs[id];
            if (this.Cost + poiCost - oldPoiCost > this.MetaData.CostBudget)
                return false;

            // Check if insertion violates open or close hours for any item
            FilledSpace space = null;
            if (pos > 0)
            {
                var prevPoi = pois[pos - 1]; // id
                var prevSpace = filledSpaces[prevPoi];
                space = this.CalculateFilledSpace(id, prevSpace, prevPoi);
            }
            else
            {
                space = this.CalculateFilledSpace(id);
            }

            if (space == null)
                return false;

            var movements = new Dictionary<int, FilledSpace>();
            int moveIndex = -1;
            for (int i = pos + 1; i < pois.Count; i++)
            {
                var poi = pois[i]; //id
                var nextSpace = filledSpaces[poi];

                var prevPoi = moveIndex >= 0 ? pois[i - 1] : id;
                var prevSpace = moveIndex >= 0 ? movements[prevPoi] : space;

                FilledSpace newSpace = this.CalculateFilledSpace(poi, prevSpace, prevPoi);

                if (newSpace == null)
                    return false;

                // break if no movement in current space
                if (newSpace.Start == nextSpace.Start && newSpace.End == nextSpace.End)
                    break;

                moveIndex++;
                movements[poi] = newSpace;
            }

            // Check if exceeds return time to point 0
            if (movements.Count > 0 && moveIndex == pois.Count - 1)
            {
                var last = movements.Last();
                var travelTime = this.MetaData.TravelTimes[last.Key][0];
                if (last.Value.End + travelTime > this.MetaData.TimeBudget)
                    return false;
            }
            else if (pos == pois.Count - 1)
            {
                var last = space;
                var travelTime = this.MetaData.TravelTimes[id][0];
                if (last.End + travelTime > this.MetaData.TimeBudget)
                    return false;
            }
            else
            {
                var lastId = pois.Last();
                var last = filledSpaces[lastId];
                var travelTime = this.MetaData.TravelTimes[lastId][0];
                if (last.End + travelTime > this.MetaData.TimeBudget)
                    return false;
            }

            //Safe change state
            foreach (var kv in movements)
            {
                filledSpaces[kv.Key] = kv.Value;
            }

            this.PoiTypes[id] = type;
            this.PoiTypes.Remove(oldPoi);
            this.PoiTypeCount[oldPoiType]--;
            this.PoiTypeCount[type]++;
            this.Cost += poiCost - oldPoiCost;
            this.Score += this.MetaData.Scores[id] - this.MetaData.Scores[oldPoi];
            filledSpaces.Add(id, space);
            filledSpaces.Remove(oldPoi);
            pois[pos] = id;
            this.Durations[tour] = filledSpaces[pois.Last()].End - this.MetaData.StartTime;
            this.CalculateEmptySpaces(tour);

            return true;
        }

        public FilledSpace CalculateFilledSpace(int id, FilledSpace prevSpace = null, int prevPoi = 0)
        {
            var poiDuration = this.MetaData.Durations[id];

            // Check if insertion violates open or close hours for any item
            var space = new FilledSpace()
            {
                Start = this.MetaData.StartTime, // Start time of the general tour
                End = 0
            };

            var poiOpenClose = this.MetaData.PoiWorkingHours[id];
            var travelTime = this.MetaData.TravelTimes[id][prevPoi];

            if (prevSpace != null)
            {
                space.Start = prevSpace.End;
            }

            if (poiOpenClose.From > space.Start + travelTime)
            {
                // If poi starts later than travel time after prev point,
                // then there should be a hole in time space to match the opening hour
                space.Start = (float)Math.Round(poiOpenClose.From - travelTime, 2);
            }

            if (space.Start + poiDuration + travelTime > this.MetaData.EndTime)
                return null;

            // Travel time is included in duration
            space.End = (float)Math.Round(space.Start + poiDuration + travelTime, 2);

            // Visit may last more than close hour, but must be reached (start time) before close hour
            // if (space.End > poiOpenClose.To)
            //     return null;
            if (space.Start > poiOpenClose.To)
                return null;

            return space;
        }

        public void CalculateEmptySpaces(int tour)
        {
            var pois = this.Pois[tour];
            var filledSpaces = this.FilledSpaces[tour];
            var emptySpaces = this.EmptySpaces[tour];

            emptySpaces.Clear();
            if (pois.Count == 0) return;

            var first = pois.First();
            if (filledSpaces[first].Start > this.MetaData.StartTime)
            {
                emptySpaces.Add(new EmptySpace()
                {
                    Start = this.MetaData.StartTime,
                    End = filledSpaces[first].Start - this.MetaData.StartTime,
                    After = 0
                });
            }

            for (int i = 1; i < pois.Count; i++)
            {
                var currPoi = pois[i];
                var prevPoi = pois[i - 1];
                var currSpace = filledSpaces[currPoi];
                var prevSpace = filledSpaces[prevPoi];

                if (currSpace.Start - prevSpace.End > 0)
                {
                    emptySpaces.Add(new EmptySpace()
                    {
                        Start = prevSpace.End,
                        End = currSpace.Start,
                        Before = i - 1,
                        After = i
                    });
                }
            }

            var last = pois.Last();
            if (filledSpaces[last].End < this.MetaData.EndTime)
            {
                emptySpaces.Add(new EmptySpace()
                {
                    Start = filledSpaces[last].End,
                    End = this.MetaData.EndTime,
                    Before = pois.Count - 1
                });
            }
        }

        public bool ArePoisUnique()
        {
            var set = new HashSet<int>();
            foreach (var pois in this.Pois)
            {
                foreach (var poi in pois)
                {
                    if (set.Contains(poi))
                        return false;
                    else
                        set.Add(poi);
                }
            }
            return true;
        }

        public bool IsPatternValid()
        {
            var matches = new bool[this.TourCount];

            for (int i = 0; i < this.TourCount; i++)
            {
                var pattern = this.MetaData.Patterns[i];
                var pois = this.Pois[i];
                var curr = pattern.First();

                int cursor = 0;
                foreach (var poi in pois)
                {
                    if (this.PoiTypes[poi] == curr)
                    {
                        cursor++;
                        if (cursor == pattern.Length)
                        {
                            matches[i] = true;
                            break;
                        }
                        else
                        {
                            curr = pattern[cursor];
                        }
                    }
                }
            }

            return matches.All(x => x);
        }

        public object Clone()
        {
            var clone = (Solution)this.MemberwiseClone();

            clone.Durations = (float[])this.Durations.Clone();

            clone.Pois = new List<int>[this.Pois.Length];
            for (int i = 0; i < this.Pois.Length; i++)
                clone.Pois[i] = new List<int>(this.Pois[i]);

            clone.PoiTypes = new Dictionary<int, string>();
            foreach (var item in this.PoiTypes)
                clone.PoiTypes.Add(item.Key, item.Value);

            clone.PoiTypeCount = new Dictionary<string, int>();
            foreach (var item in this.PoiTypeCount)
                clone.PoiTypeCount.Add(item.Key, item.Value);

            clone.FilledSpaces = new Dictionary<int, FilledSpace>[this.FilledSpaces.Length];
            for (int i = 0; i < this.FilledSpaces.Length; i++)
            {
                var spaces = new Dictionary<int, FilledSpace>();
                foreach (var item in this.FilledSpaces[i])
                    spaces.Add(item.Key, (FilledSpace)item.Value.Clone());
                clone.FilledSpaces[i] = spaces;
            }

            clone.EmptySpaces = new List<EmptySpace>[this.EmptySpaces.Length];
            for (int i = 0; i < this.EmptySpaces.Length; i++)
            {
                var spaces = new List<EmptySpace>();
                foreach (var item in this.EmptySpaces[i])
                    spaces.Add((EmptySpace)item.Clone());
                clone.EmptySpaces[i] = spaces;
            }

            return clone;
        }

        public string PrintSummary()
        {
            var res = "[";
            for (int i = 0; i < this.TourCount; i++)
            {
                res += "{";
                foreach (var item in this.Pois[i])
                {
                    res += $"{item}->{this.PoiTypes[item]},";
                }
                res = res.TrimEnd(',') + "},";
            }
            return res.TrimEnd(',') + $"]";
        }
    }
}