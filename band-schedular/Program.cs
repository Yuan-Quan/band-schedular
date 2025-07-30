using System;
using System.Collections.Generic;
using System.Linq;

namespace BandScheduler
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BandScheduler scheduler = new BandScheduler();
            Console.WriteLine("Band Scheduler initialized.");
            // Additional logic to interact with the scheduler can be added here
            scheduler.PrintBands();
            scheduler.AddBandsToSlots();
            scheduler.PrintSchedule();
        }
    }

    public class BandScheduler
    {
        private List<Band> Bands = BandCsvParser.ParseBandsFromCsv(@"C:\Users\Quan\Source\repos\band-schedular\CollectedPrefs.csv");
        private Performance Performance = new Performance();

        public void AddBandsToSlots()
        {
            Console.WriteLine("Adding bands to performance slots...");
            Performance.AddBandsToSlots(Bands);
        }

        public void PrintSchedule()
        {
            Console.WriteLine();
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine($"   {Performance.PerformanceName}");
            Console.WriteLine($"   Venue: {Performance.Venue}");
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine();

            foreach (var day in Performance.Days)
            {
                Console.WriteLine($"📅 {day.Date:yyyy-MM-dd} ({day.Date:dddd})");
                Console.WriteLine("─".PadRight(80, '─'));

                // Header
                Console.WriteLine($"{"Slot",-6} {"Candidates (Weight)",-70}");
                Console.WriteLine("─".PadRight(80, '─'));

                // Time slots
                for (int i = 0; i < day.TimeSlots.Count; i++)
                {
                    var slot = day.TimeSlots[i];
                    string candidatesInfo = "No candidates";

                    if (slot.BandCandidates.Count > 0)
                    {
                        var candidatesList = new List<string>();
                        foreach (var candidate in slot.BandCandidates)
                        {
                            string bandName = candidate.Band.Name;
                            // Truncate band name if too long
                            if (bandName.Length > 20)
                            {
                                bandName = bandName.Substring(0, 17) + "...";
                            }
                            candidatesList.Add($"{bandName}({candidate.PreferenceWeight})");
                        }
                        candidatesInfo = string.Join(", ", candidatesList);

                        // Truncate the entire candidates string if too long
                        if (candidatesInfo.Length > 68)
                        {
                            candidatesInfo = candidatesInfo.Substring(0, 65) + "...";
                        }
                    }

                    Console.WriteLine($"{i + 1,-6} {candidatesInfo,-70}");
                }

                Console.WriteLine();
            }

            // Summary
            Console.WriteLine("📊 Schedule Summary:");
            Console.WriteLine("─".PadRight(40, '─'));

            int totalSlots = Performance.Days.Sum(d => d.TimeSlots.Count);
            int slotsWithCandidates = Performance.Days.SelectMany(d => d.TimeSlots).Count(s => s.BandCandidates.Count > 0);
            int emptySlotsCount = totalSlots - slotsWithCandidates;
            int totalCandidates = Performance.Days.SelectMany(d => d.TimeSlots).Sum(s => s.BandCandidates.Count);

            Console.WriteLine($"Total Slots:     {totalSlots}");
            Console.WriteLine($"Slots w/ Candidates: {slotsWithCandidates}");
            Console.WriteLine($"Empty Slots:     {emptySlotsCount}");
            Console.WriteLine($"Total Candidates: {totalCandidates}");
            Console.WriteLine($"Total Bands:     {Bands.Count}");
            Console.WriteLine();
        }

        public void PrintBands()
        {
            Console.WriteLine("Bands in the scheduler:");
            foreach (var band in Bands)
            {
                Console.WriteLine($"Band Name: {band.Name}, GUID: {band.GUID},");
                for (int i = 0; i < band.SchedulePreferences.Count; i++)
                {
                    var pref = band.SchedulePreferences[i];
                    Console.WriteLine($"    Pref{i + 1}: {pref.PreferredDate.ToString("yyyy-MM-dd")} Slot {pref.PreferredTimeSlot + 1}");
                }
            }
        }

    }
}
