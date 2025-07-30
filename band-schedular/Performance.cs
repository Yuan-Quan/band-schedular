using System;
using System.Collections.Generic;

namespace BandScheduler
{
    public class Performance
    {
        public String PerformanceName { get; set; } = "笙伙2025SummerFest";
        public string Venue { get; set; } = "Modern Sky Lab Kunming";
        public List<PerformanceDay> Days { get; set; }

        public Performance()
        {
            Days = new List<PerformanceDay>();
            InitializeDays();
        }

        private void InitializeDays()
        {
            // August 11, 2025 - 7 time slots (0-6)
            var day1 = new PerformanceDay
            {
                Date = new DateTime(2025, 8, 11)
            };
            for (int i = 0; i < 7; i++)
            {
                day1.TimeSlots.Add(new TimeSlot { Order = i });
            }
            Days.Add(day1);

            // August 12, 2025 - 6 time slots (0-5)
            var day2 = new PerformanceDay
            {
                Date = new DateTime(2025, 8, 12)
            };
            for (int i = 0; i < 6; i++)
            {
                day2.TimeSlots.Add(new TimeSlot { Order = i });
            }
            Days.Add(day2);

            // August 13, 2025 - 6 time slots (0-5)
            var day3 = new PerformanceDay
            {
                Date = new DateTime(2025, 8, 13)
            };
            for (int i = 0; i < 6; i++)
            {
                day3.TimeSlots.Add(new TimeSlot { Order = i });
            }
            Days.Add(day3);
        }
    }

    public class PerformanceDay
    {
        public DateTime Date { get; set; }
        public List<TimeSlot> TimeSlots { get; set; } = new List<TimeSlot>();
    }

    public class TimeSlot
    {
        public int Order { get; set; } // 0 - 6 for day 1, 0-5 for day 2 and 3

        // rehearsal and performance time

        public List<Band> CandidateBands { get; set; } = new List<Band>();
    }
}
