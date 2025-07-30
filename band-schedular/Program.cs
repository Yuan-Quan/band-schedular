using System;
using System.Collections.Generic;
using System.Linq;
using Google.OrTools.LinearSolver;

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


            Console.WriteLine("\n" + "=".PadRight(80, '='));
            var optimizedPerformanceStage1 = scheduler.OptimizeGoogleORTools();
            Console.WriteLine("=".PadRight(80, '=') + "\n");

            scheduler.PrintSchedule(optimizedPerformanceStage1);

            Console.WriteLine("\n" + "=".PadRight(80, '='));
            var optimizedPerformanceStage0 = scheduler.OptimizeScheduleStage0();
            Console.WriteLine("=".PadRight(80, '=') + "\n");

            scheduler.PrintSchedule(optimizedPerformanceStage0);

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


        public Performance OptimizeGoogleORTools()
        {
            Console.WriteLine("Optimizing schedule using Google OR Tools...");

            // Create a deep copy of the performance to work with
            var optimizedPerformance = Performance.DeepCopy();

            // Create the solver
            Solver solver = Solver.CreateSolver("SCIP");
            if (solver == null)
            {
                Console.WriteLine("Could not create solver SCIP. Trying CBC...");
                solver = Solver.CreateSolver("CBC_MIXED_INTEGER_PROGRAMMING");
            }

            if (solver == null)
            {
                Console.WriteLine("Could not create solver. OR-Tools not available.");
                return Performance.DeepCopy(); // Return unchanged copy
            }

            // Create variables: decision variables for each band-slot combination
            var bandSlotVars = new Dictionary<(Band band, int dayIndex, int slotIndex), Variable>();
            var bandSlotWeights = new Dictionary<(Band band, int dayIndex, int slotIndex), double>();

            // Initialize decision variables and weights
            for (int dayIndex = 0; dayIndex < optimizedPerformance.Days.Count; dayIndex++)
            {
                var day = optimizedPerformance.Days[dayIndex];
                for (int slotIndex = 0; slotIndex < day.TimeSlots.Count; slotIndex++)
                {
                    var slot = day.TimeSlots[slotIndex];

                    foreach (var candidate in slot.BandCandidates)
                    {
                        var key = (candidate.Band, dayIndex, slotIndex);
                        // Binary variable: 1 if band is assigned to this slot, 0 otherwise
                        bandSlotVars[key] = solver.MakeIntVar(0, 1, $"band_{candidate.Band.GUID}_day_{dayIndex}_slot_{slotIndex}");
                        bandSlotWeights[key] = candidate.PreferenceWeight;
                    }
                }
            }

            Console.WriteLine($"Created {bandSlotVars.Count} decision variables.");

            // Constraint 1: Each band can be assigned to at most one slot
            foreach (var band in Bands)
            {
                var bandVars = bandSlotVars.Where(kvp => kvp.Key.band == band).Select(kvp => kvp.Value).ToArray();
                if (bandVars.Length > 0)
                {
                    var constraint = solver.MakeConstraint(0, 1, $"band_{band.GUID}_max_one_slot");
                    foreach (var variable in bandVars)
                    {
                        constraint.SetCoefficient(variable, 1);
                    }
                }
            }

            // Constraint 2: Each slot can have at most one band assigned
            for (int dayIndex = 0; dayIndex < optimizedPerformance.Days.Count; dayIndex++)
            {
                var day = optimizedPerformance.Days[dayIndex];
                for (int slotIndex = 0; slotIndex < day.TimeSlots.Count; slotIndex++)
                {
                    var slotVars = bandSlotVars.Where(kvp => kvp.Key.dayIndex == dayIndex && kvp.Key.slotIndex == slotIndex)
                                               .Select(kvp => kvp.Value).ToArray();
                    if (slotVars.Length > 0)
                    {
                        var constraint = solver.MakeConstraint(0, 1, $"day_{dayIndex}_slot_{slotIndex}_max_one_band");
                        foreach (var variable in slotVars)
                        {
                            constraint.SetCoefficient(variable, 1);
                        }
                    }
                }
            }

            Console.WriteLine($"Added constraints for {Bands.Count} bands and {optimizedPerformance.Days.Sum(d => d.TimeSlots.Count)} slots.");

            // Objective: Maximize total weight of assignments
            var objective = solver.Objective();
            foreach (var kvp in bandSlotVars)
            {
                var weight = bandSlotWeights[kvp.Key];
                objective.SetCoefficient(kvp.Value, weight);
            }
            objective.SetMaximization();

            Console.WriteLine("Starting optimization...");
            var resultStatus = solver.Solve();

            if (resultStatus == Solver.ResultStatus.OPTIMAL)
            {
                Console.WriteLine($"Optimal solution found! Total weight: {solver.Objective().Value()}");

                // Clear current assignments
                foreach (var day in optimizedPerformance.Days)
                {
                    foreach (var slot in day.TimeSlots)
                    {
                        slot.BandCandidates.Clear();
                    }
                }

                // Apply optimal assignments
                int assignedBands = 0;
                foreach (var kvp in bandSlotVars)
                {
                    if (kvp.Value.SolutionValue() > 0.5) // Variable is set to 1
                    {
                        var (band, dayIndex, slotIndex) = kvp.Key;
                        var weight = bandSlotWeights[kvp.Key];

                        var day = optimizedPerformance.Days[dayIndex];
                        var slot = day.TimeSlots[slotIndex];

                        slot.BandCandidates.Add(new BandCandidate(band, weight));

                        assignedBands++;
                        Console.WriteLine($"  - {band.Name} assigned to {day.Date:MM-dd} slot {GetSlotDisplayName(slot)} (weight: {weight})");
                    }
                }

                Console.WriteLine($"Successfully assigned {assignedBands} bands to their optimal slots.");
            }
            else if (resultStatus == Solver.ResultStatus.FEASIBLE)
            {
                Console.WriteLine($"Feasible solution found. Total weight: {solver.Objective().Value()}");
                Console.WriteLine("Note: This may not be the optimal solution.");
            }
            else
            {
                Console.WriteLine($"No solution found. Status: {resultStatus}");
            }

            // Cleanup
            solver.Dispose();

            return optimizedPerformance;
        }

        public Performance OptimizeScheduleStage0()
        {
            Console.WriteLine("Optimizing schedule - Stage 1: Removing lower weighted preferences for uncontested top choices...");

            // Create a deep copy of the performance to work with
            var optimizedPerformance = Performance.DeepCopy();
            int totalRemovedPreferences = 0;

            foreach (var day in optimizedPerformance.Days)
            {
                foreach (var slot in day.TimeSlots)
                {
                    if (slot.BandCandidates.Count == 0) continue; // Skip empty slots

                    // Handle single candidate slots (automatically uncontested)
                    if (slot.BandCandidates.Count == 1)
                    {
                        var winningBand = slot.BandCandidates[0].Band;
                        var winningWeight = slot.BandCandidates[0].PreferenceWeight;
                        int removedForThisBand = 0;

                        // Remove all lower-weighted preferences for this winning band from OTHER slots
                        foreach (var otherDay in optimizedPerformance.Days)
                        {
                            foreach (var otherSlot in otherDay.TimeSlots)
                            {
                                if (otherSlot == slot) continue; // Skip the current winning slot

                                // Find candidates for this band in other slots
                                var bandCandidatesInOtherSlot = otherSlot.BandCandidates
                                    .Where(bc => bc.Band == winningBand && bc.PreferenceWeight < winningWeight)
                                    .ToList();

                                // Remove lower-weighted preferences
                                foreach (var candidateToRemove in bandCandidatesInOtherSlot)
                                {
                                    otherSlot.BandCandidates.Remove(candidateToRemove);
                                    removedForThisBand++;
                                    totalRemovedPreferences++;
                                }
                            }
                        }

                        if (removedForThisBand > 0)
                        {
                            Console.WriteLine($"  - {winningBand.Name} secured {day.Date:MM-dd} slot {GetSlotDisplayName(slot)} " +
                                            $"(weight {winningWeight}) - removed {removedForThisBand} lower preferences");
                        }
                        continue; // Move to next slot
                    }

                    // Handle multi-candidate slots
                    // Group candidates by band (in case a band appears multiple times)
                    var bandGroups = slot.BandCandidates
                        .GroupBy(bc => bc.Band)
                        .Select(g => new
                        {
                            Band = g.Key,
                            MaxWeight = g.Max(bc => bc.PreferenceWeight),
                            Candidates = g.ToList()
                        })
                        .ToList();

                    // Find the highest weight among all bands for this slot
                    double highestWeight = bandGroups.Max(bg => bg.MaxWeight);

                    // Get bands that have the highest weight for this slot
                    var topWeightBands = bandGroups
                        .Where(bg => bg.MaxWeight == highestWeight)
                        .ToList();

                    // If only one band has the highest weight, it's uncontested
                    if (topWeightBands.Count == 1)
                    {
                        var winningBand = topWeightBands[0].Band;
                        int removedForThisBand = 0;

                        // Remove all lower-weighted preferences for this winning band from OTHER slots
                        foreach (var otherDay in optimizedPerformance.Days)
                        {
                            foreach (var otherSlot in otherDay.TimeSlots)
                            {
                                if (otherSlot == slot) continue; // Skip the current winning slot

                                // Find candidates for this band in other slots
                                var bandCandidatesInOtherSlot = otherSlot.BandCandidates
                                    .Where(bc => bc.Band == winningBand && bc.PreferenceWeight < highestWeight)
                                    .ToList();

                                // Remove lower-weighted preferences
                                foreach (var candidateToRemove in bandCandidatesInOtherSlot)
                                {
                                    otherSlot.BandCandidates.Remove(candidateToRemove);
                                    removedForThisBand++;
                                    totalRemovedPreferences++;
                                }
                            }
                        }

                        Console.WriteLine($"  - {winningBand.Name} secured {day.Date:MM-dd} slot {GetSlotDisplayName(slot)} " +
                                        $"(weight {highestWeight}) - removed {removedForThisBand} lower preferences");
                    }
                }
            }

            // Re-sort all slots after optimization
            foreach (var day in optimizedPerformance.Days)
            {
                foreach (var slot in day.TimeSlots)
                {
                    slot.BandCandidates = slot.BandCandidates
                        .OrderByDescending(bc => bc.PreferenceWeight)
                        .ToList();
                }
            }

            Console.WriteLine($"Stage 1 optimization complete. Removed {totalRemovedPreferences} lower-weighted preferences.");

            return optimizedPerformance;
        }

        private string GetSlotDisplayName(TimeSlot slot)
        {
            return slot.IsFlexibleSlot ? "Flex" : (slot.Order + 1).ToString();
        }

        public void PrintSchedule(Performance? performance = null)
        {
            var performanceToUse = performance ?? Performance;
            Console.WriteLine();
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine($"   {performanceToUse.PerformanceName}");
            Console.WriteLine($"   Venue: {performanceToUse.Venue}");
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine();

            foreach (var day in performanceToUse.Days)
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
                    string slotLabel;

                    if (slot.IsFlexibleSlot)
                    {
                        slotLabel = "Flex";
                    }
                    else
                    {
                        slotLabel = (i + 1).ToString();
                    }

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

                    Console.WriteLine($"{slotLabel,-6} {candidatesInfo,-70}");
                }
                Console.WriteLine();
            }

            // Summary
            Console.WriteLine("📊 Schedule Summary:");
            Console.WriteLine("─".PadRight(40, '─'));

            int totalSlots = performanceToUse.Days.Sum(d => d.TimeSlots.Count);
            int slotsWithCandidates = performanceToUse.Days.SelectMany(d => d.TimeSlots).Count(s => s.BandCandidates.Count > 0);
            int emptySlotsCount = totalSlots - slotsWithCandidates;
            int totalCandidates = performanceToUse.Days.SelectMany(d => d.TimeSlots).Sum(s => s.BandCandidates.Count);

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
                    string slotDisplay = pref.PreferredTimeSlot == -1 ? "Flex" : $"Slot {pref.PreferredTimeSlot + 1}";
                    Console.WriteLine($"    Pref{i + 1}: {pref.PreferredDate.ToString("yyyy-MM-dd")} {slotDisplay}");
                }
            }
        }

    }
}
