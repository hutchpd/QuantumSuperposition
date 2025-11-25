using PositronicVariables.Attributes;
using PositronicVariables.Variables;
using PositronicVariables.Runtime;
using System;
using System.Linq;

internal static class Program
{
    // Deterministic seed for collapse selection
    private const int CollapseSeed = 42;

    [DontPanic]
    private static void Main()
    {
        // Fixed future constraint: task finishes today at 16:00 local time.
        DateTime targetFinish = DateTime.Today.AddHours(16);

        // Positronic variables (time-travelling schedule components)
        var finishTime = PositronicVariable<DateTime>.GetOrCreate("FinishTime", targetFinish);
        var durationHours = PositronicVariable<int>.GetOrCreate("DurationHours", 1); // bootstrap guess
        var startTime = PositronicVariable<DateTime>.GetOrCreate("StartTime", targetFinish.AddHours(-1));

        // Print the final and correct result before we do any work because time travel.
        Console.WriteLine("Schedule:");
        Console.WriteLine($"  Duration (hours): {durationHours.ToValues().Single()}");
        Console.WriteLine($"  Start: {startTime.ToValues().Single():HH:mm}");
        Console.WriteLine($"  Finish: {finishTime.ToValues().Single():HH:mm}");

        // Step 1: Explore multiple possible durations (union of timelines).
        // We simply assign both possibilities; engine unions them across reverse / forward cycles.
        durationHours.Assign(1, 2);

        // Step 2: Apply rule StartTime = FinishTime - DurationHours for ALL duration possibilities.
        DateTime f = finishTime.ToValues().Last();
        var possibleStarts = durationHours.ToValues()
                                          .Select(d => f.AddHours(-d))
                                          .Distinct()
                                          .ToArray();
        startTime.Assign(possibleStarts);

        // Step 3: Deterministic collapse once we have both options present.
        bool hasDurationBranching = durationHours.GetCurrentQBit().States.Count > 1;
        bool hasStartBranching = startTime.GetCurrentQBit().States.Count > 1;
        if (hasDurationBranching && hasStartBranching)
        {
            // Use seeded observation to select one consistent timeline.
            var rng = new Random(CollapseSeed);

            int chosenDuration = durationHours.GetCurrentQBit().Observe(rng);
            DateTime chosenStart = f.AddHours(-chosenDuration);

            // Collapse / commit chosen branch.
            durationHours.Assign(chosenDuration);
            startTime.Assign(chosenStart);

            // Unify to single slice so future prints show the resolved schedule.
            durationHours.UnifyAll();
            startTime.UnifyAll();
            finishTime.UnifyAll();
        }
    }
}
