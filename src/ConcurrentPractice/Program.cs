using System.Text;
using ConcurrentPractice.Examples;

Console.OutputEncoding = Encoding.UTF8;

var examples = new (string Title, Func<Task> Run)[]
{
    ("Why concurrent collections? (List vs lock vs Concurrent)", WhyConcurrent.RunAsync),
    ("ConcurrentDictionary basics (check-then-act)",             DictionaryBasics.RunAsync),
    ("The GetOrAdd trap (and the Lazy<T> fix)",                  GetOrAddTrap.RunAsync),
    ("ConcurrentQueue, ConcurrentStack, ConcurrentBag",          QueueStackBag.RunAsync),
    ("The nested collection trap",                               NestedCollectionTrap.RunAsync),
    ("BlockingCollection (sync producer-consumer)",              BlockingCollectionExample.RunAsync),
    ("Channels (async producer-consumer)",                       ChannelBasics.RunAsync),
    ("Bounded channels and backpressure",                        BoundedChannel.RunAsync),
};

// If a number is passed on the command line, run that example directly: dotnet run -- 3
if (args.Length > 0 && int.TryParse(args[0], out var direct) && direct >= 1 && direct <= examples.Length)
{
    await RunExample(direct);
    return;
}

while (true)
{
    Console.WriteLine();
    Console.WriteLine("=== CONCURRENT PRACTICE ===");
    for (int i = 0; i < examples.Length; i++)
        Console.WriteLine($"{i + 1}) {examples[i].Title}");
    Console.WriteLine("0) Exit");
    Console.Write("Choice: ");

    var input = Console.ReadLine();
    if (input is null || input == "0") break;

    if (int.TryParse(input, out var choice) && choice >= 1 && choice <= examples.Length)
        await RunExample(choice);
    else
        Console.WriteLine("Invalid choice.");
}

async Task RunExample(int number)
{
    var example = examples[number - 1];
    Console.WriteLine();
    Console.WriteLine($"--- {number}) {example.Title} ---");
    await example.Run();
}
