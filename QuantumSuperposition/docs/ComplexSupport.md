## Working with Complex Numbers

QuantumSuperposition supports complex numbers for advanced quantum operations. This section will guide you through the usage of complex numbers in the library.

### Basic Complex Number Operations

You can perform basic arithmetic operations on complex numbers within a superposition. Here’s an example:

```csharp
var complex1 = new QuBit<Complex>(new Complex(1, 2));
var complex2 = new QuBit<Complex>(new Complex(3, 4));

var sum = complex1 + complex2;
var difference = complex1 - complex2;
var product = complex1 * complex2;
var quotient = complex1 / complex2;

Console.WriteLine($"Sum: {sum}");
Console.WriteLine($"Difference: {difference}");
Console.WriteLine($"Product: {product}");
Console.WriteLine($"Quotient: {quotient}");
```

### Complex Number Superpositions

You can create superpositions of complex numbers and perform operations on them. Here’s an example:

```csharp
var complexSuperposition1 = new QuBit<Complex>(new[] { new Complex(1, 2), new Complex(3, 4) });
var complexSuperposition2 = new QuBit<Complex>(new[] { new Complex(5, 6), new Complex(7, 8) });

var superpositionSum = complexSuperposition1 + complexSuperposition2;
var superpositionProduct = complexSuperposition1 * complexSuperposition2;

Console.WriteLine($"Superposition Sum: {superpositionSum}");
Console.WriteLine($"Superposition Product: {superpositionProduct}");
```

### Weighted Complex Superpositions

You can also create weighted superpositions of complex numbers. Here’s an example:

```csharp
var weightedComplexSuperposition = new QuBit<Complex>(
    new Dictionary<Complex, Complex>
    {
        { new Complex(1, 2), new Complex(0.6, 0.8) },
        { new Complex(3, 4), new Complex(0.3, 0.4) },
        { new Complex(5, 6), new Complex(0.1, 0.2) }
    });

var weightedSum = weightedComplexSuperposition + new Complex(1, 1);

Console.WriteLine($"Weighted Sum: {weightedSum}");
```

### Observing Complex Superpositions

You can observe complex superpositions to get a single collapsed value. Here’s an example:

```csharp
var complexSuperposition = new QuBit<Complex>(new[] { new Complex(1, 2), new Complex(3, 4), new Complex(5, 6) });

var observedValue = complexSuperposition.Observe();

Console.WriteLine($"Observed Value: {observedValue}");
```

### Complex Number Functions

You can apply complex number functions to superpositions. Here’s an example:

```csharp
var complexSuperposition = new QuBit<Complex>(new[] { new Complex(1, 2), new Complex(3, 4) });

var magnitudeSuperposition = complexSuperposition.Select(c => c.Magnitude);
var phaseSuperposition = complexSuperposition.Select(c => c.Phase);

Console.WriteLine($"Magnitude Superposition: {magnitudeSuperposition}");
Console.WriteLine($"Phase Superposition: {phaseSuperposition}");
```
