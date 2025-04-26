// PositronicQuickPathBugTests.cs
using NUnit.Framework;
using QuantumSuperposition.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[TestFixture]
public class PositronicQuickPathBugTests
{
    private IPositronicRuntime _rt;

    [SetUp]
    public void fresh() =>
        _rt = new ServiceCollection()
                 .AddPositronicRuntime()
                 .BuildServiceProvider()
                 .GetRequiredService<IPositronicRuntime>();

    /* ------------------------------------------------------------------ *
     *  H Y P O T H E S I S   ①                                           *
     * ------------------------------------------------------------------ */

    [Test]
    public void QuickPath_SecondAssign_ShouldMergeNotOverwrite()
    {
        // forward-only context, *outside* convergence-loop
        PositronicVariable<int>.SetEntropy(_rt, +1);

        var v = PositronicVariable<int>.GetOrCreate("q", 0, _rt);
        v.Assign(1);          // should yield any(0,1)
        v.Assign(2);          // should yield any(0,1,2)

        var values = v.ToValues().OrderBy(x => x).ToArray();

        /* EXPECTED   = { 0,1,2 }
         * ACTUAL NOW = { 2 }                                 ⟵ should fail
         */
        Assert.That(values, Is.EquivalentTo(new[] { 0, 1, 2 }),
            "Quick-path forward writes overwrite instead of merging.");
    }

    /* ------------------------------------------------------------------ *
     *  H Y P O T H E S I S   ②                                           *
     * ------------------------------------------------------------------ */

    [Test]
    public void QuickPath_WritesAreNotRolledBackByFinalUndo()
    {
        // 1️⃣ forward-only, single assignment
        PositronicVariable<int>.SetEntropy(_rt, +1);
        var v = PositronicVariable<int>.GetOrCreate("x", 0, _rt);
        v.Assign(1);

        // 2️⃣ simulate what ConvergenceEngine does at the *very* end:
        OperationLog.ReverseLastOperations();

        // If the write had been logged, Undo() would have restored the 0.
        var final = v.ToValues().Single();

        /* EXPECTED   = 0
         * ACTUAL NOW = 1                                       ⟵ should fail
         */
        Assert.That(final, Is.EqualTo(0),
            "The final reverse pass did not undo the quick-path overwrite—" +
            "suggests the write was never recorded.");
    }

    /* ------------------------------------------------------------------ *
     *  H Y P O T H E S I S   ③                                           *
     * ------------------------------------------------------------------ */

    [Test]
    public void UnifyCycle_FailsOnlyBecauseEarlierValuesWereDropped()
    {
        //  ❶  replicate exactly the original arrange
        var v = PositronicVariable<int>.GetOrCreate("probe", 0, _rt);
        v.Assign(1);
        v.Assign(2);               // timeline intended to be [0,1,2]

        //  ❷  sanity-check before calling Unify
        var before = v.ToValues().OrderBy(x => x).ToArray();
        Assert.That(before, Is.EquivalentTo(new[] { 0, 1, 2 }),
            "Pre-condition failed: values already missing *before* Unify().");

        //  ❸  call the API under test
        v.Unify(3);

        //  ❹  it should still be the same set
        var after = v.ToValues().OrderBy(x => x).ToArray();
        Assert.That(after, Is.EquivalentTo(new[] { 0, 1, 2 }),
            "Unify(count) must preserve the whole set.");
    }
}
