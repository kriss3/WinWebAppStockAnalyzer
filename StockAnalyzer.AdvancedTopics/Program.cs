using System.Diagnostics;

using static System.Console;

namespace StockAnalyzer.AdvancedTopics;

internal class Program
{
	static readonly object lockObject = new();  

	static readonly object lock1 = new();
	static readonly object lock2 = new();

	static ThreadLocal<decimal?> threadLocal = new();
	static AsyncLocal<decimal?> asyncLocal = new();

	static async Task Main()
	{
		//UsingInterlockedType();

		//await IntroductionToDeadlock();

		//WorkingWithCancellation();

		// Working with ThreadLocal<T> and AsyncLocal<T>

		//WorkingWithThreadLocalOfT();

		var optionParallel = new ParallelOptions
		{
			MaxDegreeOfParallelism = 2
		};
		asyncLocal.Value = 200;
		Parallel.For(0, 100, optionParallel, (i) =>
		{
			var currentValue = asyncLocal.Value;
			asyncLocal.Value = Compute(i);
		});
		var currentValue = asyncLocal.Value;
	}

	private static void WorkingWithThreadLocalOfT()
	{
		var optionParallel = new ParallelOptions
		{
			MaxDegreeOfParallelism = 2
		};
		Parallel.For(0, 100, optionParallel, (i) =>
		{
			var currentValue = threadLocal.Value;
			threadLocal.Value = Compute(i);
		});
	}

	private static void WorkingWithCancellation()
	{
		var stopwatch = new Stopwatch();
		stopwatch.Start();

		var cancellationTokenSource = new CancellationTokenSource();
		cancellationTokenSource.CancelAfter(2000);

		var parallelOptions = new ParallelOptions
		{
			CancellationToken = cancellationTokenSource.Token,
			MaxDegreeOfParallelism = 1
		};

		int total = 0;
		try
		{
			Parallel.For(0, 100, parallelOptions, (i) =>
			{
			});
		}
		catch (OperationCanceledException ex)
		{

			WriteLine("Cancellation requested!");
		}


		WriteLine($"Total: {total}");
		WriteLine($"It took: {stopwatch.ElapsedMilliseconds}ms to run.");
		ReadLine();
	}

	private static async Task IntroductionToDeadlock()
	{
		Stopwatch stopwatch = new();
		stopwatch.Start();
		// avoid nested locks and sharing locks.

		var t1 = Task.Run(() =>
		{
			lock (lock1)
			{
				Thread.Sleep(1);
				lock (lock2)
				{
					WriteLine("Thread 1: Holding lock 1 & 2");
				}
			}

		});

		var t2 = Task.Run(() =>
		{
			lock (lock2)
			{
				Thread.Sleep(1);
				lock (lock1)
				{
					WriteLine("Thread 2: Holding lock 1 & 2");
				}
			}

		});

		await Task.WhenAll(t1, t2);

		WriteLine($"It took: {stopwatch.ElapsedMilliseconds}ms to run");
		ReadLine();
	}

	private static void UsingInterlockedType()
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

	readonly static Random random = new();
    static decimal Compute(int value)
    {
        var randomMilliseconds = random.Next(10, 50);
        var end = DateTime.Now + TimeSpan.FromMilliseconds(randomMilliseconds);

        // This will spin for a while...
        while(DateTime.Now < end) { }

        return value + 0.5m;
    }
}