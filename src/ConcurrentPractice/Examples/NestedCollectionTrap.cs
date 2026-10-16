using System.Collections.Concurrent;

namespace ConcurrentPractice.Examples;

/// <summary>
/// TOPIC: The nested collection trap
/// ConcurrentDictionary only protects ITS OWN structure (adding, removing, reading keys).
/// The objects stored inside it are NOT made thread-safe.
/// "The cabinet is locked, but the notebook inside is open to everyone."
/// </summary>
public static class NestedCollectionTrap
{
    private const int OrderCount = 100_000;

    public static Task RunAsync()
    {
        // 1) ❌ ConcurrentDictionary<string, List<T>>: the inner List is a plain List
        var wrong = new ConcurrentDictionary<string, List<int>>();
        try
        {
            Parallel.For(0, OrderCount, i => wrong.GetOrAdd("table1", _ => new List<int>()).Add(i));
            Print("Inner List", wrong["table1"].Count);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{"Inner List",-22}: 💥 {((ex as AggregateException)?.InnerExceptions[0] ?? ex).GetType().Name}");
        }

        // 2) ✅ Make the inner collection concurrent as well
        var right = new ConcurrentDictionary<string, ConcurrentQueue<int>>();
        Parallel.For(0, OrderCount, i => right.GetOrAdd("table1", _ => new ConcurrentQueue<int>()).Enqueue(i));
        Print("Inner ConcurrentQueue", right["table1"].Count);

        return Task.CompletedTask;
    }

    private static void Print(string title, int count)
    {
        var status = count == OrderCount ? "✅ correct" : $"❌ {OrderCount - count:N0} orders lost!";
        Console.WriteLine($"{title,-22}: {count,8:N0}  {status}");
    }
}
