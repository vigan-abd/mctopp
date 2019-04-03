using System;
using System.Collections.Generic;
using System.Linq;

namespace MCTOPP.Models.Algorithm
{
    public class FilledSpace
    {
        public float Start { get; set; }
        public float End { get; set; }
        public float Size { get => End - Start; }
    }

    public class EmptySpace : FilledSpace
    {
        public int Before { get; set; } = -1;
        public int After { get; set; } = -1;
    }

    public class Solution
    {
        public float Cost { get; set; }

        public float Duration { get; set; }

        /// <summary>Poi identifiers for each tour</summary>
        public List<int> Pois { get; set; }

        /// <summary>Poi type of each item, {id, type} keypair</summary>
        public Dictionary<int, int> PoiTypes { get; set; }

        /// <summary>Count for each poi type inside solution</summary>
        public Dictionary<int, int> PoiTypeCount { get; set; }

        public Dictionary<int, FilledSpace> FilledSpaces { get; set; }

        public List<EmptySpace> EmptySpaces { get; set; }

        protected MetaData MetaData { get; set; }

        public Solution(int tourCount, MetaData metaData)
        {
            this.MetaData = metaData;

            this.Pois = new List<int>();
            this.PoiTypes = new Dictionary<int, int>();
            this.PoiTypeCount = new int[10].Select((x, i) => i + 1).ToDictionary(k => k, v => 0);

            this.FilledSpaces = new Dictionary<int, FilledSpace>();
            this.EmptySpaces = new List<EmptySpace>()
            {
                new EmptySpace
                {
                    Start = this.MetaData.StartTime,
                    End = this.MetaData.EndTime
                }
            };
        }

        public bool Insert(int id, int type, int pos)
        {
            // Check if position is inside range
            if (pos > this.Pois.Count)
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
                var prevPoi = this.Pois[pos - 1]; // id
                var prevSpace = this.FilledSpaces[prevPoi];
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
            for (int i = pos; i < this.Pois.Count; i++)
            {
                var poi = this.Pois[i]; //id
                var nextSpace = this.FilledSpaces[poi];

                var prevPoi = moveIndex >= 0 ? this.Pois[i - 1] : id;
                var prevSpace = moveIndex >= 0 ? movements[prevPoi] : space;

                // break if no overlap, no need to recalc remaining
                if (nextSpace.Start >= prevSpace.End)
                    break;

                FilledSpace newSpace = this.CalculateFilledSpace(poi, prevSpace, prevPoi);

                if (newSpace == null)
                    return false;

                moveIndex++;
                movements[poi] = newSpace;
            }

            foreach (var kv in movements)
            {
                this.FilledSpaces[kv.Key] = kv.Value;
            }

            this.PoiTypes[id] = type;
            this.PoiTypeCount[type]++;
            this.Cost += poiCost;
            this.FilledSpaces.Add(id, space);
            this.Pois.Insert(pos, id);
            this.Duration = this.FilledSpaces[this.Pois.Last()].End - this.MetaData.StartTime;
            this.CalculateEmptySpaces();

            return true;
        }

        public bool Remove(int pos)
        {
            var movements = new Dictionary<int, FilledSpace>();
            var moveIndex = -1;
            for (int i = pos + 1; i < this.Pois.Count; i++)
            {
                var poi = this.Pois[i]; //id
                var nextSpace = this.FilledSpaces[poi];

                FilledSpace newSpace = null;
                if (i == 1)
                {
                    newSpace = this.CalculateFilledSpace(poi);
                }
                else
                {
                    var prevPoi = moveIndex >= 0 ? this.Pois[i - 1] : this.Pois[pos - 1];
                    var prevSpace = moveIndex >= 0 ? movements[prevPoi] : this.FilledSpaces[prevPoi];
                    newSpace = this.CalculateFilledSpace(poi, prevSpace, prevPoi);
                }

                if (newSpace == null)
                    return false;

                moveIndex++;
                movements[poi] = newSpace;
            }

            foreach (var kv in movements)
            {
                this.FilledSpaces[kv.Key] = kv.Value;
            }

            var deleteId = this.Pois[pos];
            var deletePoiCost = this.MetaData.Costs[deleteId];
            var deletePoiType = this.PoiTypes[deleteId];

            this.PoiTypeCount[deletePoiType]--;
            this.Cost -= deletePoiCost;
            this.FilledSpaces.Remove(deleteId);
            this.PoiTypes.Remove(deleteId);
            this.Pois.RemoveAt(pos);
            this.Duration = this.FilledSpaces[this.Pois.Last()].End - this.MetaData.StartTime;
            this.CalculateEmptySpaces();
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


            if (space.End > poiOpenClose.To)
                return null;

            return space;
        }

        public void CalculateEmptySpaces()
        {
            this.EmptySpaces.Clear();
            if (this.Pois.Count == 0) return;

            var first = this.Pois.First();
            if (this.FilledSpaces[first].Start > this.MetaData.StartTime)
            {
                this.EmptySpaces.Add(new EmptySpace()
                {
                    Start = this.MetaData.StartTime,
                    End = this.FilledSpaces[first].Start - this.MetaData.StartTime,
                    Before = 0
                });
            }

            for (int i = 1; i < this.Pois.Count; i++)
            {
                var currPoi = this.Pois[i];
                var prevPoi = this.Pois[i - 1];
                var currSpace = this.FilledSpaces[currPoi];
                var prevSpace = this.FilledSpaces[prevPoi];

                if (currSpace.Start - prevSpace.End > 0)
                {
                    this.EmptySpaces.Add(new EmptySpace()
                    {
                        Start = prevSpace.End,
                        End = currSpace.Start,
                        Before = i,
                        After = i - 1
                    });
                }
            }

            var last = this.Pois.Last();
            if (this.FilledSpaces[last].End < this.MetaData.EndTime)
            {
                this.EmptySpaces.Add(new EmptySpace()
                {
                    Start = this.FilledSpaces[last].End,
                    End = this.MetaData.EndTime,
                    Before = -1,
                    After = this.Pois.Count - 1
                });
            }
        }
    }
}