internal static class Program
{
    [PositronicEntry]
    private static void Main()
    {
        var greeting = PositronicVariableRef<string>.GetOrCreate("greeting", "Hello");
        Console.WriteLine($"Current greeting: {greeting}");

        var nextGreeting = greeting.ToValues().First() switch
        {
            "Hello" => "Hi",
            "Hi" => "Hey",
            "Hey" => "Hello",
            _ => "Hello"
        };

        greeting.Assign(nextGreeting);
    }
}