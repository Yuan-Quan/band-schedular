using System;
using System.Collections.Generic;

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
        }
    }

    public class BandScheduler
    {
        private List<Band> bands = BandCsvParser.ParseBandsFromCsv(@"C:\Users\Quan\Source\repos\band-schedular\CollectedPrefs.csv");
        private List<Performance> schedule = new List<Performance>();

        public void PrintBands()
        {
            Console.WriteLine("Bands in the scheduler:");
            foreach (var band in bands)
            {
                Console.WriteLine($"Band Name: {band.Name}, GUID: {band.GUID},");
                for (int i = 0; i < band.SchedulePreferences.Count; i++)
                {
                    var pref = band.SchedulePreferences[i];
                    Console.WriteLine($"    Pref{i + 1}: {pref.PreferredDate.ToString()} {pref.PreferredTimeSlot}");
                }
            }
        }

    }
}
