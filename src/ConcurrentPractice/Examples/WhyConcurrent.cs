using System.Collections.Concurrent;

namespace ConcurrentPractice.Examples;

/// <summary>
/// TOPIC: Why do concurrent collections exist?
/// List, Dictionary and Queue are NOT thread-safe.
/// If multiple threads write at the same time, items get lost or an exception is thrown.
/// Old fix: wrap every access in a lock. New fix: let the collection do the locking.
/// </summary>
public static class WhyConcurrent
{
    private const int OrderCount = 100_000;

    public static Task RunAsync()
    {
        // 1) ❌ Plain List: 10 waiters writing into the same order book at once
        var list = new List<int>();
        try
        {
            Parallel.For(0, OrderCount, i => list.Add(i));
            Print("List (no lock)", list.Count);
        }
        catch (Exception ex)
        {
            // Sometimes the internal array gets corrupted while resizing and an exception is thrown.
            // Parallel.For wraps it in an AggregateException, so we show the real (inner) one.
            Console.WriteLine($"{"List (no lock)",-22}: 💥 {((ex as AggregateException)?.InnerExceptions[0] ?? ex).GetType().Name}");
        }

        // 2) ✅ List + lock: correct, but every thread waits at the same single door
        var lockedList = new List<int>();
        var gate = new object();
        Parallel.For(0, OrderCount, i =>
        {
            lock (gate)
            {
                lockedList.Add(i);
            }
        });
        Print("List + lock", lockedList.Count);

        // 3) ✅ ConcurrentQueue: the collection handles thread safety itself
        var queue = new ConcurrentQueue<int>();
        Parallel.For(0, OrderCount, i => queue.Enqueue(i));
        Print("ConcurrentQueue", queue.Count);

        return Task.CompletedTask;
    }

    private static void Print(string title, int count)
    {
        var status = count == OrderCount ? "✅ correct" : $"❌ {OrderCount - count:N0} orders lost!";
        Console.WriteLine($"{title,-22}: {count,8:N0}  {status}");
    }
}
