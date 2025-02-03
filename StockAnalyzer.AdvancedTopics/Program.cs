using System.Diagnostics;

using static System.Console;

namespace StockAnalyzer.AdvancedTopics;

internal class Program
{
    static void Main()
    {
        Stopwatch stopwatch = new();
        stopwatch.Start();

        WriteLine($"It took: {stopwatch.ElapsedMilliseconds}ms to run");
        ReadLine();
    }

    static Random random = new();
    static decimal Compute(int value)
    {
        var randomMilliseconds = random.Next(10, 50);
        var end = DateTime.Now + TimeSpan.FromMilliseconds(randomMilliseconds);

        // This will spin for a while...
        while(DateTime.Now < end) { }

        return value + 0.5m;
    }
}