using System.Collections.Concurrent;

namespace ConcurrentPractice.Examples;

/// <summary>
/// TOPIC: ConcurrentDictionary
/// Rule: a single method call is atomic, but a combination of two calls is NOT.
/// "Check then act" (read -> decide -> write) must be done in ONE method:
/// TryAdd, GetOrAdd, AddOrUpdate, TryUpdate, TryRemove.
/// </summary>
public static class DictionaryBasics
{
    private const int VisitCount = 100_000;

    public static Task RunAsync()
    {
        // 1) The basic Try... methods
        var stock = new ConcurrentDictionary<string, int>();

        Console.WriteLine($"TryAdd Kebab (10)      : {stock.TryAdd("Kebab", 10)}");
        Console.WriteLine($"TryAdd Kebab again (99): {stock.TryAdd("Kebab", 99)}  (already exists, nothing changed)");

        if (stock.TryGetValue("Kebab", out var kebabs))
            Console.WriteLine($"TryGetValue Kebab      : {kebabs}");

        // TryUpdate = compare-and-swap: "set it to 9 ONLY IF it is still 10"
        Console.WriteLine($"TryUpdate 10 -> 9      : {stock.TryUpdate("Kebab", 9, 10)}");
        Console.WriteLine($"TryUpdate 10 -> 8      : {stock.TryUpdate("Kebab", 8, 10)}  (value is 9 now, not 10)");

        // GetOrAdd: returns the existing value, or adds it if missing
        Console.WriteLine($"GetOrAdd Rice (5)      : {stock.GetOrAdd("Rice", 5)}");
        Console.WriteLine($"GetOrAdd Rice (50)     : {stock.GetOrAdd("Rice", 50)}  (already exists, returns the old value)");

        // AddOrUpdate: adds 1 if missing, otherwise applies the update function
        stock.AddOrUpdate("Soup", 1, (_, old) => old + 1);
        stock.AddOrUpdate("Soup", 1, (_, old) => old + 1);
        Console.WriteLine($"AddOrUpdate Soup x2    : {stock["Soup"]}");

        if (stock.TryRemove("Rice", out var removed))
            Console.WriteLine($"TryRemove Rice         : removed {removed}");

        Console.WriteLine();
        Console.WriteLine($"Counting {VisitCount:N0} page visits from many threads:");

        // 2) ❌ Check-then-act: TryGetValue and the indexer are two separate steps.
        //    Two threads may both read 5 and both write 6, so one visit is lost.
        //    The dictionary itself is not corrupted, but YOUR logic is.
        var wrong = new ConcurrentDictionary<string, int>();
        Parallel.For(0, VisitCount, _ =>
        {
            if (wrong.TryGetValue("home", out var count))
                wrong["home"] = count + 1;
            else
                wrong["home"] = 1;
        });
        Print("TryGetValue + indexer", wrong["home"]);

        // 3) ✅ AddOrUpdate does the read and the write as one atomic operation
        var right = new ConcurrentDictionary<string, int>();
        Parallel.For(0, VisitCount, _ => right.AddOrUpdate("home", 1, (_, old) => old + 1));
        Print("AddOrUpdate", right["home"]);

        return Task.CompletedTask;
    }

    private static void Print(string title, int count)
    {
        var status = count == VisitCount ? "✅ correct" : $"❌ {VisitCount - count:N0} visits lost!";
        Console.WriteLine($"  {title,-22}: {count,8:N0}  {status}");
    }
}
