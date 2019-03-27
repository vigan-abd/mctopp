using System;
using System.Collections.Generic;

namespace MCTOPP.Models.Algorithm
{
    public class Validator
    {
        public static bool IsValid(Solution solution, MetaData metaData)
        {
            if (!IsBudgetValid(solution, metaData))
                return false;
            if (!IsPatternValid(solution, metaData))
                return false;
            if (!IsTimeValid(solution, metaData))
                return false;
            if (!IsMaxPoisValid(solution, metaData))
                return false;

            return true;
        }

        public static bool IsBudgetValid(Solution solution, MetaData metaData)
        {
            var sum = 0.0f;
            foreach (var tour in solution.tours)
            {
                foreach (var item in tour)
                {
                    sum += metaData.Costs[item];
                }
            }

            return sum <= metaData.CostBudget;
        }

        public static bool IsPatternValid(Solution solution, MetaData metaData)
        {
            for (int i = 0; i < solution.tours.Length; i++)
            {
                var tour = solution.tours[i];
                var pattern = metaData.Patterns[i];

                var index = 0;
                var curr = pattern[index];

                foreach (var item in tour)
                {
                    if (metaData.PoiTypes[item] == curr)
                    {
                        index++;
                        if (index == pattern.Length)
                            break;

                        curr = pattern[index];
                    }
                }

                if (index != pattern.Length)
                    return false;
            }

            return true;
        }

        public static bool IsTimeValid(Solution solution, MetaData metaData)
        {
            for (int i = 0; i < solution.tours.Length; i++)
            {
                var tour = solution.tours[i];
                var times = solution.times[i];
                var tourDuration = 0.0f;

                var j = 0;
                foreach (var item in tour)
                {
                    var range = metaData.PoiWorkingHours[item];
                    var timeSoFar = times[j];

                    if (timeSoFar - metaData.Durations[item] < range.From || timeSoFar > range.To)
                        return false;

                    tourDuration += timeSoFar;
                    j++;
                }

                if (tourDuration > metaData.TimeBudget)
                    return false;
            }

            return true;
        }

        public static bool IsMaxPoisValid(Solution solution, MetaData metaData)
        {
            for (int i = 0; i < solution.tours.Length; i++)
            {
                var tour = solution.tours[i];
                var counters = new Dictionary<int, int>();
                foreach (var kv in metaData.MaxPoisOfType)
                    counters.Add(kv.Key, 0);

                foreach (var item in tour)
                    counters[item]++;


                foreach (var kv in metaData.MaxPoisOfType)
                {
                    if (kv.Value < counters[kv.Key])
                        return false;
                }
            }

            return true;
        }
    }
}