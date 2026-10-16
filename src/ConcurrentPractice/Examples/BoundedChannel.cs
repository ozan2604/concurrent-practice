using System.Diagnostics;
using System.Threading.Channels;

namespace ConcurrentPractice.Examples;

/// <summary>
/// TOPIC: Bounded channels, backpressure and multiple consumers
/// Unbounded: if the producer is faster than the consumer, the queue grows forever -> out of memory.
/// Bounded: when the channel is full, WriteAsync waits. This is called BACKPRESSURE:
///          the slow consumer naturally slows down the fast producer.
/// FullMode decides what happens when it is full: Wait (default), DropOldest, DropNewest, DropWrite.
/// </summary>
public static class BoundedChannel
{
    public static async Task RunAsync()
    {
        await BackpressureWithMultipleChefs();
        Console.WriteLine();
        await DropOldest();
    }

    // 1) Capacity 2, one fast waiter, three slow chefs
    private static async Task BackpressureWithMultipleChefs()
    {
        Console.WriteLine("Bounded(2) + FullMode.Wait, 1 waiter, 3 chefs:");
        var channel = Channel.CreateBounded<int>(new BoundedChannelOptions(2)
        {
            FullMode = BoundedChannelFullMode.Wait
        });
        var sw = Stopwatch.StartNew();

        var waiter = Task.Run(async () =>
        {
            for (int i = 1; i <= 9; i++)
            {
                // Waits here whenever the window already holds 2 orders
                await channel.Writer.WriteAsync(i);
                Console.WriteLine($"  [{sw.ElapsedMilliseconds,4} ms] Waiter wrote Order {i}");
            }
            channel.Writer.Complete();
        });

        // Each order goes to exactly ONE chef. The work is shared out automatically.
        var chefs = Enumerable.Range(1, 3).Select(id => Task.Run(async () =>
        {
            await foreach (var order in channel.Reader.ReadAllAsync())
            {
                await Task.Delay(500); // cooking takes time
                Console.WriteLine($"  [{sw.ElapsedMilliseconds,4} ms]     Chef {id} cooked Order {order}");
            }
        }));

        await Task.WhenAll(chefs.Append(waiter));
        Console.WriteLine("Notice: the waiter could not run ahead; it moved at the chefs' pace (backpressure).");
    }

    // 2) DropOldest: useful for live data (e.g. sensor readings) where only the latest values matter
    private static async Task DropOldest()
    {
        Console.WriteLine("Bounded(3) + FullMode.DropOldest, writing 1..10 with no reader:");
        var channel = Channel.CreateBounded<int>(new BoundedChannelOptions(3)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

        // Never waits: when full, the oldest item is thrown away to make room
        for (int i = 1; i <= 10; i++)
            await channel.Writer.WriteAsync(i);
        channel.Writer.Complete();

        var kept = new List<int>();
        await foreach (var reading in channel.Reader.ReadAllAsync())
            kept.Add(reading);

        Console.WriteLine($"  Kept: {string.Join(", ", kept)}  (only the newest 3 survived)");
    }
}
