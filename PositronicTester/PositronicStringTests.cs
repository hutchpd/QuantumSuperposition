using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QuantumSuperposition.QuantumSoup;
using QuantumSuperposition.Core;

namespace PositronicVariables.Tests
{
    [TestFixture]
    public class PositronicVariableRefStringTests
    {
        [SetUp]
        public void SetUp()
        {
            // Reset all static variables and set the quantum 'entropy'
            PositronicVariableRef<string>.ResetStaticVariables();
            PositronicVariableRef<string>.SetEntropy(-1);
        }

        [Test]
        public void StringCycleAndUnifyTest()
        {
            // Create a PositronicVariableRef with an initial greeting.
            var greeting = PositronicVariableRef<string>.GetOrCreate("greeting", "Hello");

            // Cycle through greetings.
            greeting.Assign("Hi");
            greeting.Assign("Hey");
            greeting.Assign("Hello");

            // Unify the timeline so that the union is calculated.
            greeting.UnifyAll();

            // Expect the union to contain all three distinct greetings.
            var finalStates = greeting.ToValues().Distinct().OrderBy(s => s).ToList();
            Assert.That(finalStates, Is.EquivalentTo(new[] { "Hello", "Hey", "Hi" }));
            Assert.That(greeting.ToString(), Does.Contain("any("));
        }

        [Test]
        public void StringMultipleAssignmentsTimelineTest()
        {
            var greeting = PositronicVariableRef<string>.GetOrCreate("greeting", "Hello");

            // Make several assignments. Depending on the ReplaceOrAppendOrUnify logic,
            // the first assignment might replace the initial slice, and later ones append.
            greeting.Assign("Hello");  // may replace the seed if it's different
            greeting.Assign("Hi");
            greeting.Assign("Hey");

            // We expect the timeline to have at least two slices now.
            Assert.That(greeting.timeline.Count, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void StringForkIndependenceTest()
        {
            var greeting = PositronicVariableRef<string>.GetOrCreate("greeting", "Hello");
            greeting.Assign("Hi");

            // Create a fork of the variable.
            var fork = greeting.Fork();

            // Change the fork independently.
            fork.Assign("Hey");

            // Unify both branches.
            greeting.UnifyAll();
            fork.UnifyAll();

            var finalOriginal = greeting.ToValues().Distinct().OrderBy(s => s).ToList();
            var finalFork = fork.ToValues().Distinct().OrderBy(s => s).ToList();

            // Depending on whether the first assignment replaced the seed,
            // the original may contain {"Hello", "Hi"} and the fork should include the additional "Hey".
            Assert.That(finalOriginal, Is.EquivalentTo(new[] { "Hello", "Hi" }));
            Assert.That(finalFork, Is.EquivalentTo(new[] { "Hello", "Hi", "Hey" }));
        }
    }
}
