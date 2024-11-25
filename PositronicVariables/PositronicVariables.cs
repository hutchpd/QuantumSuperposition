using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public class PositronicVariable<T> where T : struct, IComparable
{
    // Static variables
    private static List<PositronicVariable<T>> _antivars = new List<PositronicVariable<T>>();
    private static int _entropy = -1;
    private static bool _converged = false;
    private static int _convergence = 0;
    private static Action _mainLogic;

    // Instance variables
    private List<Eigenstates<T>> _timeline = new List<Eigenstates<T>>();
    private int _antistate = 0;

    public Eigenstates<T> Value { get; private set; }

    public static void ResetStaticVariables()
    {
        _antivars = new List<PositronicVariable<T>>();
        _entropy = -1;
        _converged = false;
        _convergence = 0;
        _mainLogic = null;
    }

    public PositronicVariable(T initialValue)
    {
        Value = new Eigenstates<T>(new List<T> { initialValue });
        _timeline.Add(Value);
        _antivars.Add(this);
    }
    public static void SetEntropy(int value)
    {
        _entropy = value;
    }

    public static int GetEntropy()
    {
        return _entropy;
    }


    // Operator overloading for operations with T
    public static PositronicVariable<T> operator +(PositronicVariable<T> a, T b)
    {
        var result = new PositronicVariable<T>(default(T));
        result.Value = a.Value + b;
        return result;
    }

    public static PositronicVariable<T> operator %(PositronicVariable<T> a, T b)
    {
        var result = new PositronicVariable<T>(default(T));
        result.Value = a.Value % b;
        return result;
    }

    // Assignment method since we can't overload '='
    public void Assign(PositronicVariable<T> other)
    {
        var val = other.Value;

        if (_entropy > 0)
        {
            // Time running forward: add new universe to timelines
            Value = val;
            _timeline.Add(Value);
        }
        else if (_convergence > 1)
        {
            // Timelines have converged: use entire convergence loop of timelines
            var superstates = new List<T>();
            int maxState = _timeline.Max(tl => tl.ToValues().Count()) - 1;

            for (int stateNum = 0; stateNum <= maxState; stateNum++)
            {
                var states = new List<T>();

                for (int i = _timeline.Count - _convergence; i < _timeline.Count; i++)
                {
                    var timelineValues = _timeline[i].ToValues().ToList();
                    if (stateNum < timelineValues.Count)
                        states.Add(timelineValues[stateNum]);
                }

                var anyState = new Eigenstates<T>(states.Distinct());
                superstates.AddRange(anyState.ToValues());
            }

            Value = new Eigenstates<T>(superstates.Distinct());
            _timeline.Add(Value);
            _antistate = 0;
        }
        else
        {
            // Time running backward: use most recent timeline
            Value = _timeline.Last();
            _antistate = 0;
        }
    }

    public int Converged()
    {
        Console.WriteLine($"Converged called. Timeline count: {_timeline.Count}");

        if (_timeline.Count < 3)
            return 0;

        var currTl = _timeline[_timeline.Count - 2];
        int maxState = _timeline.Max(tl => tl.ToValues().Count()) - 1;

        for (int timelineNum = 3; timelineNum <= _timeline.Count; timelineNum++)
        {
            var prevTl = _timeline[_timeline.Count - timelineNum];
            bool allEqual = true;

            for (int stateNum = 0; stateNum <= maxState; stateNum++)
            {
                T currVal = default(T), prevVal = default(T);
                var currValues = currTl.ToValues().ToList();
                var prevValues = prevTl.ToValues().ToList();

                if (stateNum < currValues.Count)
                    currVal = currValues[stateNum];

                if (stateNum < prevValues.Count)
                    prevVal = prevValues[stateNum];

                // Comparing values
                if (!EqualityComparer<T>.Default.Equals(currVal, prevVal))
                {
                    allEqual = false;
                    break;
                }
            }

            if (allEqual)
                return timelineNum - 2;
        }

        return 0;
    }


    public override string ToString()
    {
        return Value.ToString();
    }

    /// <summary>
    ///  Static methods for managing the convergence loop
    /// </summary>
    /// <param name="mainLogic"></param>
    public static void RunConvergenceLoop(Action mainLogic)
    {
        _mainLogic = mainLogic;
        _converged = false;
        int iteration = 0;
        int maxIterations = 1000; // Prevent infinite loops

        // Start capturing console output
        StartCapture();

        while (!_converged && iteration < maxIterations)
        {
            ReverseArrowOfTime();
            Console.WriteLine($"Iteration: {iteration}, Entropy: {_entropy}");


            // Run the main logic
            _currentOutput.GetStringBuilder().Clear();
            _mainLogic.Invoke();

            // Capture console output
            CaptureConsoleOutput();

            // Check for convergence when time is moving backward
            if (_entropy < 0)
            {
                var convergences = _antivars.Select(v => v.Converged()).ToList();
                int minConvergence = convergences.Min();
                _converged = minConvergence > 0;
                if (_converged)
                    _convergence = minConvergence;
            }

            iteration++;
        }

        // Stop capturing console output
        StopCapture();

        if (_converged)
        {
            // Once converged, restore console output and run forward one last time
            _entropy = 1;
            _currentOutput.GetStringBuilder().Clear();
            StartCapture();
            _mainLogic.Invoke();
            StopCapture();

            // Output the final result
            Console.Write(_currentOutput.ToString());
        }
        else
        {
            // Convergence not achieved within max iterations
            Console.WriteLine("Convergence not achieved.");
        }
    }

    private static void ReverseArrowOfTime()
    {
        _entropy = -_entropy;
    }

    private static TextWriter _originalConsoleOut = Console.Out;
    private static StringWriter _currentOutput = new StringWriter();

    private static void StartCapture()
    {
        _originalConsoleOut = Console.Out;
        Console.SetOut(_currentOutput);
    }

    private static void StopCapture()
    {
        Console.SetOut(_originalConsoleOut);
    }

    private static void CaptureConsoleOutput()
    {
        // No need to store outputs for this implementation, as we output only after convergence
    }
}
