using System.Threading.Channels;

namespace ConcurrentPractice.Examples;

/// <summary>
/// TOPIC: Channels (producer-consumer, the async way)
/// Channel = the kitchen window of the async world.
/// The Writer drops orders in, the Reader takes them out, FIFO.
/// When the window is empty, the chef waits with await: the thread is NOT blocked.
/// "The job waits, the worker doesn't."
/// </summary>
public static class ChannelBasics
{
    public static async Task RunAsync()
    {
        var channel = Channel.CreateUnbounded<string>();

        // Producer: the waiter
        var waiter = Task.Run(async () =>
        {
            for (int i = 1; i <= 5; i++)
            {
                await channel.Writer.WriteAsync($"Order {i}");
                Console.WriteLine($"  Waiter wrote Order {i}");
                await Task.Delay(200);
            }

            // "No more orders are coming."
            // ⚠️ If you forget Complete(), the consumer below waits forever.
            channel.Writer.Complete();
            Console.WriteLine("  Waiter: completed the channel.");
        });

        // Consumer: the chef
        var chef = Task.Run(async () =>
        {
            // Waits (without blocking) while the channel is empty.
            // The loop ends by itself once Complete() is called and the channel is drained.
            await foreach (var order in channel.Reader.ReadAllAsync())
                Console.WriteLine($"  Chef is cooking {order}  [Thread {Environment.CurrentManagedThreadId}]");
        });

        await Task.WhenAll(waiter, chef);
        Console.WriteLine("Notice: the chef's Thread Id may change, because no thread was held while waiting.");
    }
}
