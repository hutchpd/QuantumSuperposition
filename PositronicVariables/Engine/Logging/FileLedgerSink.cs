using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PositronicVariables.Engine.Logging
{
    /// <summary>
    /// Writes append-only audit entries to disk while preserving the in-memory ledger behaviour used by reverse replay.
    /// </summary>
    public sealed class FileLedgerSink : ILedgerSink
    {
        private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = false };
        private readonly ILedgerSink _innerSink;
        private readonly object _fileLock = new();

        public FileLedgerSink(string filePath, ILedgerSink innerSink = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            FilePath = Path.GetFullPath(filePath);
            string directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _innerSink = innerSink ?? new LedgerSink();
        }

        public string FilePath { get; }

        public void Append(IOperation op, Guid commitId)
        {
            _innerSink.Append(op, commitId);

            if (op == null)
            {
                return;
            }

            AppendAuditEntry(new FileLedgerAuditEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                CommitId = commitId,
                OperationName = op.OperationName,
                OperationType = op.GetType().FullName ?? op.GetType().Name
            });
        }

        public IOperation Peek() => _innerSink.Peek();

        public void Pop() => _innerSink.Pop();

        public void ReverseLastOperations() => _innerSink.ReverseLastOperations();

        public void Clear() => _innerSink.Clear();

        public IReadOnlyList<FileLedgerAuditEntry> ReadAuditTrail()
        {
            if (!File.Exists(FilePath))
            {
                return Array.Empty<FileLedgerAuditEntry>();
            }

            lock (_fileLock)
            {
                return File.ReadLines(FilePath)
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Select(line => JsonSerializer.Deserialize<FileLedgerAuditEntry>(line, s_jsonOptions))
                    .Where(entry => entry != null)
                    .ToList();
            }
        }

        private void AppendAuditEntry(FileLedgerAuditEntry entry)
        {
            string json = JsonSerializer.Serialize(entry, s_jsonOptions);
            lock (_fileLock)
            {
                File.AppendAllText(FilePath, json + Environment.NewLine);
            }
        }
    }

    public sealed class FileLedgerAuditEntry
    {
        public DateTimeOffset Timestamp { get; set; }
        public Guid CommitId { get; set; }
        public string OperationName { get; set; } = string.Empty;
        public string OperationType { get; set; } = string.Empty;
    }
}