using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

namespace BandScheduler
{
    public class Band
    {
        public string Name { get; set; }
        public string GUID { get; set; }

        public List<SchedulePreference> SchedulePreferences { get; set; } = new List<SchedulePreference>();

        public Band(string name, string guid)
        {
            Name = name;
            GUID = guid;
        }
    }

    public class SchedulePreference
    {
        public DateTime PreferredDate { get; set; } // Nullable when bands don't have preferences
        public int PreferredTimeSlot { get; set; } // also nullable, 0 - 6 for day 1, 0-5 for day 2 and 3
        public double Weight { get; set; }
        public SchedulePreference(DateTime preferredDate, int preferredTimeSlot, double weight)
        {
            PreferredDate = preferredDate;
            PreferredTimeSlot = preferredTimeSlot;
            this.Weight = weight;
        }
    }

    public class BandCsvParser
    {
        // parse csv
        public static List<Band> ParseBandsFromCsv(string csvFilePath)
        {
            if (string.IsNullOrEmpty(csvFilePath) || !File.Exists(csvFilePath))
            {
                throw new FileNotFoundException("CSV file not found.");
            }

            var bands = new List<Band>();
            var lines = File.ReadAllLines(csvFilePath);

            // Skip header line
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = ParseCsvLine(line);
                if (parts.Length < 2) continue; // Need at least BandName and GUID

                var bandName = parts[0].Trim();
                var guid = parts[1].Trim();

                if (string.IsNullOrEmpty(bandName) || string.IsNullOrEmpty(guid))
                    continue;

                var band = new Band(bandName, guid);

                // Parse preferences (up to 3 preferences)
                for (int prefIndex = 0; prefIndex < 3; prefIndex++)
                {
                    int dateIndex = 2 + (prefIndex * 2); // Pref1.date at index 2, Pref2.date at index 4, etc.
                    int slotIndex = dateIndex + 1; // Pref1.slot at index 3, Pref2.slot at index 5, etc.

                    if (dateIndex < parts.Length && slotIndex < parts.Length)
                    {
                        var dateStr = parts[dateIndex].Trim();
                        var slotStr = parts[slotIndex].Trim();

                        if (!string.IsNullOrEmpty(dateStr))
                        {
                            var parsedDate = ParseChineseDate(dateStr);
                            if (parsedDate.HasValue)
                            {
                                int timeSlot = ParseTimeSlot(slotStr);
                                double weight = 3.0 - prefIndex; // First preference gets weight 3, second gets 2, third gets 1

                                var preference = new SchedulePreference(parsedDate.Value, timeSlot, weight);
                                band.SchedulePreferences.Add(preference);
                            }
                        }
                    }
                }

                bands.Add(band);
            }

            return bands;
        }

        private static string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = "";
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current);
                    current = "";
                }
                else
                {
                    current += c;
                }
            }

            result.Add(current); // Add the last field
            return result.ToArray();
        }

        private static DateTime? ParseChineseDate(string chineseDateStr)
        {
            // Parse dates like "8月11日", "8月12日", "8月13日"
            if (string.IsNullOrEmpty(chineseDateStr)) return null;

            try
            {
                // Remove "月" and "日" characters and extract numbers
                var cleanDate = chineseDateStr.Replace("月", "/").Replace("日", "");

                // Assuming the year is 2025 (updated to match Performance class)
                var fullDateStr = $"2025/{cleanDate}";

                if (DateTime.TryParse(fullDateStr, out DateTime result))
                {
                    return result;
                }
            }
            catch
            {
                // If parsing fails, return null
            }

            return null;
        }

        private static int ParseTimeSlot(string slotStr)
        {
            if (string.IsNullOrEmpty(slotStr)) return -1; // Return -1 for no specific preference

            // Parse slots like "1st", "2nd", "3rd", "4th", "5th", "6th", "7th"
            var cleanSlot = slotStr.ToLower().Replace("st", "").Replace("nd", "").Replace("rd", "").Replace("th", "");

            if (int.TryParse(cleanSlot, out int slot))
            {
                // Convert to 0-based index (1st becomes 0, 2nd becomes 1, etc.)
                return Math.Max(0, slot - 1);
            }

            return -1; // Return -1 if parsing fails (no specific preference)
        }
    }
}


