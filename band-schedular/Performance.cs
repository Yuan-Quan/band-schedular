namespace BandScheduler
{
    public class Performance
    {
        public String PerformanceName { get; set; } = "笙伙2025SummerFest";
        public string Venue { get; set; } = "Modern Sky Lab Kunming";
        public List<DateTime> Days { get; set; }

        public Performance(Band band, DateTime date, string venue, TimeSpan startTime)
        {
            Band = band;
            Date = date;
            Venue = venue;
            StartTime = startTime;
        }
    }
}
