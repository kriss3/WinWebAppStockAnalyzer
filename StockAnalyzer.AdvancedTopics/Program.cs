using System.Diagnostics;

using static System.Console;

namespace StockAnalyzer.AdvancedTopics;

internal class Program
{
	static readonly object lockObject = new();  
	static void Main()
    {
		Stopwatch stopwatch = new();
        stopwatch.Start();

		//decimal total = 0;

		//NOTE: Always prefer atomic operation over lock when possible as it's less overhead and performs faster.
		//NOTE: Atomic operations are thread-safe and does not work on decimal , double, float, or long.
		//Parallel.For(0, 100, (i) =>
		//{
		//	var result = Compute(i); // computes first and then performs update behind closed doors in a single operation, by a single thread.
		//	lock (lockObject) //Only lock for a very short time!
		//          {
		//		total += result;
		//	}

		//NOTE: Interlock is preferred and it is faster that static lock object on a shared variables.
		//NOTE: Avoid nested locks and shared locks.

		//NOTE: Calling an expensive operation inside a lock isn't recommended as it is forcing other threads to wait.

		int total = 0;	
		Parallel.For(0, 100, (i) =>
		{
			var result = Compute(i);
			Interlocked.Add(ref total, (int)result);
		});
		stopwatch.Stop();

		WriteLine($"Total: {total}");
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