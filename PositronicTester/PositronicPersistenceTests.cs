using Microsoft.Extensions.Hosting;
using PositronicVariables.DependencyInjection;
using PositronicVariables.Engine.Logging;
using PositronicVariables.Engine.Timeline;
using PositronicVariables.Runtime;
using PositronicVariables.Transactions;
using PositronicVariables.Variables;
using QuantumSuperposition.QuantumSoup;
using System;
using System.IO;
using System.Linq;

namespace PositronicVariables.Tests
{
    [TestFixture]
    public class PositronicPersistenceTests
    {
        private IPositronicRuntime _runtime;
        private string _tempDirectory;
        private ILedgerSink _originalSink;

        [SetUp]
        public void SetUp()
        {
            var hostBuilder = new HostBuilder().ConfigureServices(s => s.AddPositronicRuntime());
            PositronicAmbient.InitialiseWith(hostBuilder);
            _runtime = PositronicAmbient.Current;
            _tempDirectory = Path.Combine(Path.GetTempPath(), "PositronicPersistenceTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);
            _originalSink = Ledger.Sink;
            Ledger.Sink = new LedgerSink();
        }

        [TearDown]
        public void TearDown()
        {
            Ledger.Sink = _originalSink;

            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }

            if (PositronicAmbient.IsInitialized && PositronicAmbient.Services is IDisposable disp)
            {
                disp.Dispose();
            }

            PositronicAmbient.PanicAndReset();
        }

        [Test]
        public void Snapshot_Save_And_Load_RoundTrips_Timeline_And_Version()
        {
            var variable = PositronicVariable<int>.GetOrCreate("snapshot-demo", 1, _runtime);
            variable.Assign(2);
            variable.Assign(3);

            var expected = variable.ExportSnapshot();
            string path = Path.Combine(_tempDirectory, "timeline.json");

            variable.SaveSnapshot(path);
            var restored = PositronicVariable<int>.LoadSnapshot(path, _runtime);
            var actual = restored.ExportSnapshot();

            Assert.That(actual.Version, Is.EqualTo(expected.Version));
            Assert.That(actual.TimelineSlices.Count, Is.EqualTo(expected.TimelineSlices.Count));
            Assert.That(actual.TimelineSlices.SelectMany(x => x), Is.EqualTo(expected.TimelineSlices.SelectMany(x => x)));
            Assert.That(((ITransactionalVariable)restored).TxVersion, Is.EqualTo(((ITransactionalVariable)variable).TxVersion));
        }

        [Test]
        public void FileLedgerSink_Writes_Audit_Entries_Without_Breaking_Replay_Ledger()
        {
            string path = Path.Combine(_tempDirectory, "ledger.jsonl");
            var sink = new FileLedgerSink(path, new LedgerSink());
            Ledger.Sink = sink;

            var variable = PositronicVariable<int>.GetOrCreate("audit-demo", 1, _runtime);
            var archivist = new BureauOfTemporalRecords<int>();

            archivist.SnapshotAppend(variable, new QuBit<int>(new[] { 2 }).Any());

            var entries = sink.ReadAuditTrail();

            Assert.That(entries, Has.Count.EqualTo(1));
            Assert.That(entries[0].OperationName, Is.EqualTo("TimelineAppend"));
            Assert.That(variable.TimelineLength, Is.EqualTo(2));
            Assert.That(Ledger.Sink.Peek(), Is.Not.Null);
        }
    }
}