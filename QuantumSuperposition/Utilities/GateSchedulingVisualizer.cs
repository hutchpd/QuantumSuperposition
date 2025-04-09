using System.Text;

public static class GateSchedulingVisualizer
{
    /// <summary>
    /// Generates an ASCII diagram of the gate operations.
    /// </summary>
    /// <param name="gateOperations">A collection of gate operations (in order).</param>
    /// <param name="totalQubits">Total number of qubits available.</param>
    /// <returns>A multiline string with the diagram.</returns>
    public static string Visualize(IEnumerable<QuantumSystem.GateOperation> gateOperations, int totalQubits)
    {
        // Determine how many time steps there are.
        var opsList = gateOperations.ToList();
        int timeSteps = opsList.Count;
        // Create a grid with one row per qubit and one column per time step.
        string[,] grid = new string[totalQubits, timeSteps];

        // Initialize grid cells with blanks.
        for (int i = 0; i < totalQubits; i++)
        {
            for (int j = 0; j < timeSteps; j++)
            {
                grid[i, j] = "    "; // four spaces (adjust width as needed)
            }
        }

        // Fill in the grid with gate labels.
        int col = 0;
        foreach (var op in opsList)
        {
            if (op.OperationType == QuantumSystem.GateType.SingleQubit)
            {
                // For single-qubit operations, place the label in its row.
                int qubit = op.TargetQubits[0];
                grid[qubit, col] = $"[{op.GateName}]";
            }
            else if (op.OperationType == QuantumSystem.GateType.TwoQubit)
            {
                // For two-qubit operations, assume TargetQubits[0] and TargetQubits[1] are the two qubit indices.
                int qA = op.TargetQubits[0];
                int qB = op.TargetQubits[1];
                // Place the label on both endpoints.
                grid[qA, col] = $"[{op.GateName}]";
                grid[qB, col] = $"[{op.GateName}]";
                // Optionally, draw a vertical connector between the two rows.
                int start = Math.Min(qA, qB);
                int end = Math.Max(qA, qB);
                for (int i = start + 1; i < end; i++)
                    grid[i, col] = " |  ";
            }
            col++;
        }

        // Build the output diagram string.
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < totalQubits; i++)
        {
            sb.Append($"q{i}: ");
            for (int j = 0; j < timeSteps; j++)
            {
                sb.Append(grid[i, j]);
                if (j < timeSteps - 1)
                    sb.Append("---");
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
