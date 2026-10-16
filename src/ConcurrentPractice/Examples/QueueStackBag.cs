using System.Collections.Concurrent;

namespace ConcurrentPractice.Examples;

/// <summary>
/// TOPIC: ConcurrentQueue, ConcurrentStack and ConcurrentBag
/// Queue = FIFO (first in, first out), Stack = LIFO (last in, first out), Bag = no order.
/// None of them wait when empty: TryDequeue / TryPop / TryTake simply return false.
/// Why "Try..."? Because "is it empty?" + "take it" would be two steps,
/// and another thread could grab the last item in between.
/// </summary>
public static class QueueStackBag
{
    public static Task RunAsync()
    {
        // 1) ConcurrentQueue: orders are cooked in the order they arrived
        var queue = new ConcurrentQueue<string>();
        queue.Enqueue("Order 1");
        queue.Enqueue("Order 2");
        queue.Enqueue("Order 3");

        Console.Write("ConcurrentQueue (FIFO): ");
        while (queue.TryDequeue(out var order))
            Console.Write($"{order}  ");
        Console.WriteLine();

        // 2) ConcurrentStack: the last plate put on the pile is taken first
        var stack = new ConcurrentStack<string>();
        stack.Push("Plate 1");
        stack.Push("Plate 2");
        stack.Push("Plate 3");

        Console.Write("ConcurrentStack (LIFO): ");
        while (stack.TryPop(out var plate))
            Console.Write($"{plate}  ");
        Console.WriteLine();

        // 3) ConcurrentBag: no order guarantee.
        //    Each thread has its own local list, so taking back what you added yourself is very fast.
        //    Good fit: object pools. Bad fit: one thread only adds, another only takes (use a queue).
        var bag = new ConcurrentBag<string>();
        Parallel.For(1, 6, i => bag.Add($"Tray {i}"));

        Console.Write("ConcurrentBag (no order): ");
        while (bag.TryTake(out var tray))
            Console.Write($"{tray}  ");
        Console.WriteLine();

        // 4) Empty collections don't wait, they just return false
        Console.WriteLine();
        Console.WriteLine($"TryDequeue on an empty queue: {queue.TryDequeue(out _)}  (returns immediately, does not wait)");
        Console.WriteLine("If you need to WAIT for new items, use BlockingCollection or a Channel.");

        // 5) Count / IsEmpty are only a snapshot: the value may change on the very next line
        Console.WriteLine($"IsEmpty: {queue.IsEmpty}  (only true at this exact moment, don't make decisions based on it)");

        return Task.CompletedTask;
    }
}
