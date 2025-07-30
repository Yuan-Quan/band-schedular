using System;
using System.Collections.Generic;
using System.Linq;

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
            // August 11, 2025 - 7 time slots (0-6) + 1 flexible slot
            var day1 = new PerformanceDay
            {
                Date = new DateTime(2025, 8, 11)
            };
            for (int i = 0; i < 7; i++)
            {
                day1.TimeSlots.Add(new TimeSlot { Order = i });
            }
            // Add flexible slot for bands without specific time preference
            day1.TimeSlots.Add(new TimeSlot { Order = -1, IsFlexibleSlot = true });
            Days.Add(day1);

            // August 12, 2025 - 6 time slots (0-5) + 1 flexible slot
            var day2 = new PerformanceDay
            {
                Date = new DateTime(2025, 8, 12)
            };
            for (int i = 0; i < 6; i++)
            {
                day2.TimeSlots.Add(new TimeSlot { Order = i });
            }
            // Add flexible slot for bands without specific time preference
            day2.TimeSlots.Add(new TimeSlot { Order = -1, IsFlexibleSlot = true });
            Days.Add(day2);

            // August 13, 2025 - 6 time slots (0-5) + 1 flexible slot
            var day3 = new PerformanceDay
            {
                Date = new DateTime(2025, 8, 13)
            };
            for (int i = 0; i < 6; i++)
            {
                day3.TimeSlots.Add(new TimeSlot { Order = i });
            }
            // Add flexible slot for bands without specific time preference
            day3.TimeSlots.Add(new TimeSlot { Order = -1, IsFlexibleSlot = true });
            Days.Add(day3);
        }

        // put all bands into the performance's slots candidates
        public void AddBandsToSlots(List<Band> bands)
        {
            // Clear existing candidates first
            foreach (var day in Days)
            {
                foreach (var slot in day.TimeSlots)
                {
                    slot.BandCandidates.Clear();
                }
            }

            // Add bands to slots based on their preferences
            foreach (var band in bands)
            {
                foreach (var preference in band.SchedulePreferences)
                {
                    // Find the matching day
                    var matchingDay = Days.FirstOrDefault(d => d.Date.Date == preference.PreferredDate.Date);

                    if (matchingDay != null)
                    {
                        // Check if band has a specific time slot preference
                        if (preference.PreferredTimeSlot >= 0 && preference.PreferredTimeSlot < matchingDay.TimeSlots.Count - 1) // -1 to exclude flexible slot
                        {
                            // Band has specific time preference
                            var targetSlot = matchingDay.TimeSlots[preference.PreferredTimeSlot];

                            var bandCandidate = new BandCandidate
                            {
                                Band = band,
                                PreferenceWeight = preference.Weight
                            };

                            targetSlot.BandCandidates.Add(bandCandidate);
                        }
                        else if (preference.PreferredTimeSlot == -1)
                        {
                            // Band has no specific time preference, add to flexible slot
                            var flexibleSlot = matchingDay.TimeSlots.FirstOrDefault(ts => ts.IsFlexibleSlot);
                            if (flexibleSlot != null)
                            {
                                var bandCandidate = new BandCandidate
                                {
                                    Band = band,
                                    PreferenceWeight = preference.Weight
                                };

                                flexibleSlot.BandCandidates.Add(bandCandidate);
                            }
                        }
                        // If PreferredTimeSlot is out of range (but not -1), ignore the preference
                    }
                }
            }

            // Sort candidates in each slot by preference weight (highest first)
            foreach (var day in Days)
            {
                foreach (var slot in day.TimeSlots)
                {
                    slot.BandCandidates = slot.BandCandidates
                        .OrderByDescending(bc => bc.PreferenceWeight)
                        .ToList();
                }
            }
        }
    }

    public class PerformanceDay
    {
        public DateTime Date { get; set; }
        public List<TimeSlot> TimeSlots { get; set; } = new List<TimeSlot>();
    }

    public class TimeSlot
    {
        public int Order { get; set; } // 0 - 6 for day 1, 0-5 for day 2 and 3, -1 for flexible slot
        public bool IsFlexibleSlot { get; set; } = false; // True for slots that accept bands without specific time preference

        // rehearsal and performance time

        public List<BandCandidate> BandCandidates { get; set; } = new List<BandCandidate>();
    }

    public class BandCandidate
    {
        public required Band Band { get; set; }
        public double PreferenceWeight { get; set; }
    }
}
