internal static class Program
{
    // Entry point into the timeline. Begins *after* knowing the result.
    [PositronicEntry]
    private static void Main()
    {
        int rowsToPrint = 10; // Limit the number of rows printed

        // Our positronic variable representing the current row in Pascal's Triangle,
        // starting with the first row [1].
        var row = PositronicVariableRef<List<int>>.GetOrCreate("row", new List<int> { 1 });

        for (int i = 0; i < rowsToPrint; i++)
        {
            // Retrieve the converged current row.
            List<int> currentRow = row.ToValues().Last();

            // Print the current row.
            Console.WriteLine(string.Join(" ", currentRow));

            // Compute the next row in the triangle.
            List<int> nextRow = ComputeNextRow(currentRow);

            // Advance the row with our positronic magic – the next row is precomputed.
            row.Assign(nextRow);
        }
    }

    // Computes the next row of Pascal's Triangle given the current row.
    private static List<int> ComputeNextRow(List<int> current)
    {
        // The next row always starts with 1.
        var next = new List<int> { 1 };

        // Each interior value is the sum of two adjacent values from the current row.
        for (int i = 0; i < current.Count - 1; i++)
        {
            next.Add(current[i] + current[i + 1]);
        }

        // The row always ends with 1.
        next.Add(1);
        return next;
    }
}
