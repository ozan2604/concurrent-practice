using System.Collections.Concurrent;

namespace ConcurrentPractice.Examples;

/// <summary>
/// TOPIC: The GetOrAdd trap (a classic interview question)
/// GetOrAdd runs the value factory OUTSIDE its lock (for performance).
/// If several threads ask for the same missing key at once, the factory can run several times.
/// Only one result is stored, the others are thrown away, but the expensive work was already done.
/// Fix: wrap the value in Lazy&lt;T&gt;, so only the stored one is ever evaluated.
/// </summary>
public static class GetOrAddTrap
{
    private const int ThreadCount = 10;
    private const int ProductId = 5;

    private static int _databaseCalls;

    public static async Task RunAsync()
    {
        // 1) ❌ Plain GetOrAdd with an expensive factory
        _databaseCalls = 0;
        var cache = new ConcurrentDictionary<int, string>();

        await RunTogether(() => cache.GetOrAdd(ProductId, LoadFromDatabase));

        Console.WriteLine($"Plain GetOrAdd : {_databaseCalls} database calls, {cache.Count} item in the cache  ❌");

        // 2) ✅ GetOrAdd + Lazy<T>
        //    Creating a Lazy is cheap, so it's fine if a few of them get created.
        //    Only the Lazy stored in the dictionary has its .Value read, so the database is hit once.
        _databaseCalls = 0;
        var lazyCache = new ConcurrentDictionary<int, Lazy<string>>();

        await RunTogether(() =>
            lazyCache.GetOrAdd(ProductId, id => new Lazy<string>(() => LoadFromDatabase(id))).Value);

        Console.WriteLine($"GetOrAdd + Lazy: {_databaseCalls} database call,  {lazyCache.Count} item in the cache  ✅");
    }

    // Starts all threads and releases them at the same moment, so they really race
    private static async Task RunTogether(Func<string> getProduct)
    {
        using var startSignal = new Barrier(ThreadCount);
        var threads = Enumerable.Range(0, ThreadCount).Select(_ => Task.Run(() =>
        {
            startSignal.SignalAndWait();
            getProduct();
        })).ToArray();
        await Task.WhenAll(threads);
    }

    // Think of a slow database query
    private static string LoadFromDatabase(int id)
    {
        Interlocked.Increment(ref _databaseCalls);
        Thread.Sleep(200);
        return $"Product {id}";
    }
}
