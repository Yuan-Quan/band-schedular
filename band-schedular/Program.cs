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
            scheduler.PrintSchedule();
        }
    }

    public class BandScheduler
    {
        private List<Band> bands = BandCsvParser.ParseBandsFromCsv(@"C:\Users\Quan\Source\repos\band-schedular\CollectedPrefs.csv");
        private Performance performance = new Performance();

        public void PrintSchedule()
        {
            Console.WriteLine();
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine($"   {performance.PerformanceName}");
            Console.WriteLine($"   Venue: {performance.Venue}");
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine();

            foreach (var day in performance.Days)
            {
                Console.WriteLine($"📅 {day.Date:yyyy-MM-dd} ({day.Date:dddd})");
                Console.WriteLine("─".PadRight(60, '─'));

                // Header
                Console.WriteLine($"{"Slot",-6} {"Band",-30} {"Status",-10}");
                Console.WriteLine("─".PadRight(60, '─'));

                // Time slots
                for (int i = 0; i < day.TimeSlots.Count; i++)
                {
                    var slot = day.TimeSlots[i];
                    string bandName = "TBD";
                    string status = "Available";

                    if (slot.CandidateBands.Count > 0)
                    {
                        bandName = slot.CandidateBands[0].Name;
                        status = "Scheduled";
                    }

                    // Truncate band name if too long
                    if (bandName.Length > 28)
                    {
                        bandName = bandName.Substring(0, 25) + "...";
                    }

                    Console.WriteLine($"{i + 1,-6} {bandName,-30} {status,-10}");
                }

                Console.WriteLine();
            }

            // Summary
            Console.WriteLine("📊 Schedule Summary:");
            Console.WriteLine("─".PadRight(40, '─'));

            int totalSlots = performance.Days.Sum(d => d.TimeSlots.Count);
            int scheduledSlots = performance.Days.SelectMany(d => d.TimeSlots).Count(s => s.CandidateBands.Count > 0);
            int availableSlots = totalSlots - scheduledSlots;

            Console.WriteLine($"Total Slots:     {totalSlots}");
            Console.WriteLine($"Scheduled:       {scheduledSlots}");
            Console.WriteLine($"Available:       {availableSlots}");
            Console.WriteLine($"Total Bands:     {bands.Count}");
            Console.WriteLine();
        }

        public void PrintBands()
        {
            Console.WriteLine("Bands in the scheduler:");
            foreach (var band in bands)
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
