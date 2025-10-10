using PositronicVariables.Attributes;
using PositronicVariables.Variables;

internal static class Program
{
    // Target row length to stop iterating
    private const int TargetRowLength = 10;

    // Entry point into the timeline. Begins *after* knowing the result.
    // Notice there is no explicit for loop here.
    [DontPanic]
    private static void Main()
    {
        // This line doesn't just create a variable. It creates a causality loop - one that stabilizes only when it finds a version of itself that matches the logical rules that come after it.
        // "Hello, I'm row. I will become a version of Pascal's Triangle when I grow up. Here is my future self: [1, 3, 3, 1]. Please backfill everything I need to become that."

        var row = PositronicVariable<ComparableList>.GetOrCreate("row", new ComparableList(1));
        // Print the final result before we do the work.
        Console.WriteLine("Final converged row: " + row);

        // “Give me the version of this variable that finally stopped fighting itself. I'll use that.”
        // The value hasn't been computed yet - it's being repeatedly derived in time.
        ComparableList currentRowWrapper = row.ToValues().OrderBy(v => v.Items.Count).Last();
        List<int> currentRow = currentRowWrapper.Items;

        if (currentRow.Count < TargetRowLength)
        {
            // We haven't reached the desired row length.
            // Compute the next row of Pascal's Triangle.
            List<int> nextRow = ComputeNextRow(currentRow);
            var nextComparable = new ComparableList(nextRow.ToArray());

            // Assign the next row.
            row.State = nextComparable;
            // (No explicit printing or looping here-the simulation will re-invoke Main() automatically.)
        }
        else
        {
            // A bit like in the 1960's movie "The Time Machine" when the time traveler pulls the lever when the world was ending.
            row.UnifyAll();
        }
    }

    /// <summary>
    /// Computes the next row of Pascal's Triangle given the current row.
    /// </summary>
    /// <param name="current">The current row as a List of int.</param>
    /// <returns>A new List of int representing the next row.</returns>
    private static List<int> ComputeNextRow(List<int> current)
    {
        // The next row always starts with 1.
        var next = new List<int> { 1 };

        // Each interior element is the sum of two adjacent numbers in the current row.
        for (int i = 0; i < current.Count - 1; i++)
        {
            next.Add(current[i] + current[i + 1]);
        }

        // The row always ends with 1.
        next.Add(1);
        return next;
    }
}


/// <summary>
/// We implement a custom IComparable<ComparableList>. Why? Because convergence requires comparison. If time  can't tell when a value has changed, it can't collapse reality into a stable state and you loop forever.
/// </summary>
public class ComparableList : IComparable<ComparableList>
{
    public List<int> Items { get; }

    public ComparableList(params int[] values)
    {
        Items = new List<int>(values);
    }

    /// <summary>
    /// This wrapper isn't just syntactic sugar. It's the contract that keeps reality from fracturing.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public int CompareTo(ComparableList other)
    {
        if (other == null) return 1;
        int countCompare = Items.Count.CompareTo(other.Items.Count);
        if (countCompare != 0) return countCompare;

        for (int i = 0; i < Items.Count; i++)
        {
            int cmp = Items[i].CompareTo(other.Items[i]);
            if (cmp != 0) return cmp;
        }
        return 0;
    }

    public override bool Equals(object obj)
    {
        return obj is ComparableList cl && CompareTo(cl) == 0;
    }

    public override int GetHashCode()
    {
        return Items.Aggregate(17, (acc, val) => acc * 31 + val);
    }

    public override string ToString()
    {
        // Format like "1 4 6 4 1"
        return string.Join(" ", Items);
    }
}



//internal static class SelfSolvingPuzzle
//{
//    [PositronicEntry]
//    internal static void Main()
//    {
//        // Create a positronic variable that doesn't know its value yet.
//        var mysteryNumber = AntiVal.GetOrCreate<int>();

//        // We declare that the result of doubling this mystery number must equal 10.
//        var doubled = AntiVal.GetOrCreate<Double>();
//        doubled.Assign(mysteryNumber * 2);

//        Console.WriteLine($"The mystery number is: {mysteryNumber}");
//        Console.WriteLine($"When doubled, it becomes: {doubled}");

//        // Here's the punchline: assign the known output
//        doubled.Assign(10);
//    }
//}