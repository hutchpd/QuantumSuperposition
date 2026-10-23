using QuantumSuperposition.Systems;
using System.Text;

namespace QuantumSuperposition.Utilities
{
    public static class GateSchedulingVisualiser
    {
        /// <summary>
        /// Generates an ASCII diagram of the gate operations.
        /// </summary>
        /// <param name="gateOperations">A collection of gate operations (in order).</param>
        /// <param name="totalQubits">Total number of qubits available.</param>
        /// <returns>A multiline string with the diagram.</returns>
        public static string Visualise(IEnumerable<QuantumSystem.GateOperation> gateOperations, int totalQubits)
        {
            // Determine how many time steps and rows there are.
            List<QuantumSystem.GateOperation> opsList = gateOperations.ToList();
            int timeSteps = opsList.Count;
            int maxTargetQubit = opsList
                .Where(op => op.TargetQubits is { Length: > 0 })
                .SelectMany(op => op.TargetQubits)
                .DefaultIfEmpty(-1)
                .Max();
            int rowCount = Math.Max(totalQubits, maxTargetQubit + 1);

            // Create a grid with one row per qubit and one column per time step.
            string[,] grid = new string[rowCount, timeSteps];

            // Initialize grid cells with blanks.
            for (int i = 0; i < rowCount; i++)
            {
                for (int j = 0; j < timeSteps; j++)
                {
                    grid[i, j] = "    "; // four spaces (adjust width as needed)
                }
            }

            // Fill in the grid with gate labels.
            int col = 0;
            foreach (QuantumSystem.GateOperation? op in opsList)
            {
                if (op.OperationType == QuantumSystem.GateType.SingleQubit && op.TargetQubits.Length >= 1)
                {
                    // For single-qubit operations, place the label in its row.
                    int qubit = op.TargetQubits[0];
                    grid[qubit, col] = $"[{op.GateName}]";
                }
                else if (op.OperationType == QuantumSystem.GateType.TwoQubit && op.TargetQubits.Length >= 2)
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
                    {
                        grid[i, col] = " |  ";
                    }
                }
                else if (op.OperationType == QuantumSystem.GateType.MultiQubit && op.TargetQubits.Length >= 1)
                {
                    int start = op.TargetQubits.Min();
                    int end = op.TargetQubits.Max();
                    for (int i = start + 1; i < end; i++)
                    {
                        grid[i, col] = " |  ";
                    }
                    foreach (int q in op.TargetQubits)
                    {
                        grid[q, col] = $"[{op.GateName}]";
                    }
                }
                col++;
            }

            // Build the output diagram string.
            StringBuilder sb = new();
            for (int i = 0; i < rowCount; i++)
            {
                _ = sb.Append($"q{i}: ");
                for (int j = 0; j < timeSteps; j++)
                {
                    _ = sb.Append(grid[i, j]);
                    if (j < timeSteps - 1)
                    {
                        _ = sb.Append("---");
                    }
                }
                _ = sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}