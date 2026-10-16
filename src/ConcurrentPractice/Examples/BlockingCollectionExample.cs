using System.Collections.Concurrent;

namespace ConcurrentPractice.Examples;

/// <summary>
/// TOPIC: BlockingCollection (producer-consumer, the synchronous way)
/// Waiters (producers) pin orders on the board, chefs (consumers) take and cook them.
/// Board empty -> the chef WAITS. Board full -> the waiter WAITS.
/// ⚠️ While waiting it BLOCKS the thread (just like .Result). In async code, use a Channel instead.
/// </summary>
public static class BlockingCollectionExample
{
    public static async Task RunAsync()
    {
        // boundedCapacity: at most 3 orders fit on the board
        using var board = new BlockingCollection<string>(boundedCapacity: 3);

        // Producer: the waiter takes orders quickly
        var waiter = Task.Run(() =>
        {
            for (int i = 1; i <= 6; i++)
            {
                board.Add($"Order {i}"); // blocks if the board is full
                Console.WriteLine($"  Waiter pinned Order {i}  (on the board: {board.Count})");
            }

            // "No more orders are coming." Without this, the chef would wait forever.
            board.CompleteAdding();
            Console.WriteLine("  Waiter: no more orders.");
        });

        // Consumer: the chef is slower than the waiter
        var chef = Task.Run(() =>
        {
            // Blocks while the board is empty, ends after CompleteAdding when the board is drained
            foreach (var order in board.GetConsumingEnumerable())
            {
                Thread.Sleep(300); // cooking takes time
                Console.WriteLine($"  Chef cooked {order}");
            }
        });

        await Task.WhenAll(waiter, chef);
        Console.WriteLine("Notice: the waiter had to wait whenever the board was full (3 orders).");
    }
}
