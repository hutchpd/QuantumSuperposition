
# QuantumSuperposition

`QuantumSuperposition` is a .NET library that introduces quantum mechanics-like superpositions into C#. Inspired by concepts in quantum computing, it allows developers to work with scalar values as if they are in multiple states simultaneously. This library is useful for advanced logical and mathematical operations, where parallelism and state superposition can simplify complex computations.

## Why QuantumSuperposition?

In quantum mechanics, a superposition is a combination of all possible states that a quantum system can be in. Similarly, the `QuantumSuperposition<T>` class allows a variable to exist in multiple states at once, enabling parallel computations and complex logical operations to be expressed in a simple, declarative manner.

This library is particularly useful in scenarios where you want to perform operations that are inherently parallel or involve decision-making based on multiple potential states. For example, finding prime numbers, calculating factors, or determining the minimum value from a set of numbers.

## Features

- **Superposition Types**: Supports `Any` (disjunctive) and `All` (conjunctive) superpositions.
- **Mathematical Operations**: Supports standard arithmetic operations (`+`, `-`, `*`, `/`, `%`) between superpositions and scalar values.
- **Comparison Operations**: Allows comparisons between superpositions and scalar values, as well as between two superpositions.
- **Eigenstates Filtering**: Provides methods to filter and retrieve states that match specific conditions.
- **Parallel Processing Simulation**: Mimics parallel processing of states, providing a simple way to express complex logical and mathematical operations.

## Getting Started

### Installation

You can install the `QuantumSuperposition` package via NuGet:

```bash
dotnet add package QuantumSuperposition
```

Or, in the NuGet Package Manager Console:

```bash
Install-Package QuantumSuperposition
```

## Usage

Here are some example applications of the `QuantumSuperposition` class:

### 1. Checking for Prime Numbers

The `IsPrime` method checks whether a number is prime by creating a `QuantumSuperposition<int>` of potential divisors and determining if the number is divisible by any of them.

```csharp
static bool IsPrime(int i)
{
    var divisors = new QuBit<int>(Enumerable.Range(2, i > 2 ? i - 2 : 1));
    return (i % divisors).All(); 
}

// Example Usage
for (int i = 1; i <= 100; i++)
{
    if (IsPrime(i))
    {
        Console.WriteLine(i + " is prime!");
    }
}
```

### 2. Finding Factors of a Number

The `Factors` method uses the `QuantumSuperposition<int>` class to determine the factors of a number by finding divisors that result in a modulus of 0.

```csharp
static Eigenstates<int> Factors(int v)
{
    var divisors = new Eigenstates<int>(Enumerable.Range(1, v));
    return v % divisors == 0;
}

// Example Usage
Console.WriteLine("Factors of 10: " + Factors(10).ToString());
```

### 3. Finding the Minimum Value in a Set

The `MinValue` method finds the minimum value in a set of integers using `QuantumSuperposition<int>` and comparison operators.

```csharp
static int MinValue(IEnumerable<int> range)
{
    var minval = new Eigenstates<int>(range);
    var test = minval.Any() <= minval.All();
    return test.ToValues().First();
}

// Example Usage
Console.WriteLine("Minimum value of 3, 5, 8 is " + MinValue(new[] { 3, 5, 8 }));
```

## Advanced Concepts

### Superposition Types

- **Any (Disjunctive Superposition)**: Represents a state where any one of the possible states could be true. This is useful for conditions that are satisfied if any state is valid.
- **All (Conjunctive Superposition)**: Represents a state where all possible states must be true. This is useful for conditions that are satisfied only when every state meets the criteria.

### Mathematical and Logical Operations

You can perform standard mathematical operations between superpositions or between a superposition and a scalar value. Similarly, you can perform logical comparisons between them. The result of these operations is a new superposition representing the outcomes of the operation applied to each state.

## Conclusion

`QuantumSuperposition` is a powerful library that brings quantum mechanics-inspired superpositions to .NET programming. It provides a simple and expressive way to perform complex logical and mathematical operations. Whether you are exploring quantum computing concepts or building advanced parallel processing algorithms, `QuantumSuperposition` offers a unique and intuitive approach to managing multiple potential states.

## Contributing

Contributions are welcome! If you find a bug or want to add a new feature, feel free to open an issue or submit a pull request on GitHub.

## License

This project is licensed under the Unlicense license - see the [LICENSE](LICENSE) file for details.

## Contact

For any questions or suggestions, please contact us at [support@findonsoftware.com].

## Acknowledgements

This library, `QuantumSuperposition`, is inspired by the concepts introduced in Damian Conway's Perl module [`Quantum::Superpositions`](https://metacpan.org/pod/Quantum::Superpositions). While this work is a new and original implementation for C#, it is heavily based on the ideas outlined in Damian Conway's work on quantum-like superpositions in Perl.

We extend our gratitude to Damian Conway for his pioneering ideas and for making them available to the programming community.
