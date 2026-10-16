# 🧺 Concurrent Practice (.NET 8)

A practice project for **`System.Collections.Concurrent`** and **`System.Threading.Channels`** in C#, explained with a restaurant analogy.

> 🍽️ **Analogy:** A plain `List` is an order book used by one waiter at a time. If 10 waiters write in it at once, the pages get mixed up. A concurrent collection is an order board with a ticket system: everyone can add and take safely at the same time.

## 🚀 Running

```bash
dotnet run --project src/ConcurrentPractice        # pick from the menu
dotnet run --project src/ConcurrentPractice -- 3   # run example 3 directly
```

## 📚 Topics

| # | Topic | File | What it shows |
|---|-------|------|---------------|
| 1 | Why concurrent? | [WhyConcurrent.cs](src/ConcurrentPractice/Examples/WhyConcurrent.cs) | `List` loses items or crashes; `lock` vs `ConcurrentQueue` |
| 2 | ConcurrentDictionary | [DictionaryBasics.cs](src/ConcurrentPractice/Examples/DictionaryBasics.cs) | `TryAdd`, `TryUpdate`, `GetOrAdd`, `AddOrUpdate`, the check-then-act bug |
| 3 | GetOrAdd trap | [GetOrAddTrap.cs](src/ConcurrentPractice/Examples/GetOrAddTrap.cs) | The factory runs many times; the `Lazy<T>` fix |
| 4 | Queue / Stack / Bag | [QueueStackBag.cs](src/ConcurrentPractice/Examples/QueueStackBag.cs) | FIFO, LIFO, no order, the `Try...` pattern |
| 5 | Nested collection trap | [NestedCollectionTrap.cs](src/ConcurrentPractice/Examples/NestedCollectionTrap.cs) | A `List` inside a `ConcurrentDictionary` is still not thread-safe |
| 6 | BlockingCollection | [BlockingCollectionExample.cs](src/ConcurrentPractice/Examples/BlockingCollectionExample.cs) | Sync producer-consumer, `CompleteAdding` |
| 7 | Channels | [ChannelBasics.cs](src/ConcurrentPractice/Examples/ChannelBasics.cs) | Async producer-consumer, `WriteAsync`, `ReadAllAsync`, `Complete` |
| 8 | Bounded channels | [BoundedChannel.cs](src/ConcurrentPractice/Examples/BoundedChannel.cs) | Backpressure, multiple consumers, `DropOldest` |

## 📊 Sample Output

```
List (no lock)        : 💥 ArgumentException   (or lost items, it changes on every run)
List + lock           :  100,000  ✅ correct
ConcurrentQueue       :  100,000  ✅ correct

TryGetValue + indexer :   21,034  ❌ 78,966 visits lost!
AddOrUpdate           :  100,000  ✅ correct

Plain GetOrAdd : 10 database calls, 1 item in the cache  ❌
GetOrAdd + Lazy: 1 database call,  1 item in the cache  ✅

Inner List            :   32,781  ❌ 67,219 orders lost!
Inner ConcurrentQueue :  100,000  ✅ correct

Bounded(3) + FullMode.DropOldest, writing 1..10 with no reader:
  Kept: 8, 9, 10  (only the newest 3 survived)
```

## 🧠 Quick Summary

| Class | Plain counterpart | When to use it |
|---|---|---|
| `ConcurrentDictionary<K,V>` | `Dictionary` | Shared cache, counters, key-value data |
| `ConcurrentQueue<T>` | `Queue` (FIFO) | Work processed in arrival order, logs |
| `ConcurrentStack<T>` | `Stack` (LIFO) | Last in, first out |
| `ConcurrentBag<T>` | unordered list | Order doesn't matter, object pools |
| `BlockingCollection<T>` | — | Sync producer-consumer (blocks the thread while waiting) |
| `Channel<T>` | — | Async producer-consumer (waits with `await`, thread stays free) |

> **One rule to remember:** a single method call is atomic, but a combination of two calls is NOT.
> If you "read → decide → write", move it into one method: `AddOrUpdate`, `GetOrAdd`, `TryUpdate`.

## ⚠️ Common Mistakes

1. **Check-then-act:** `if (dict.TryGetValue(...)) dict[k] = v + 1;` is a race condition. Use `AddOrUpdate`.
2. **`Count`, `IsEmpty`, `ToArray()` are only snapshots.** The value may change on the very next line.
3. **`ConcurrentDictionary.Count` is expensive:** it locks every segment. Don't call it in a loop.
4. **Concurrent is not always faster:** on a single thread it is slower than a plain collection.
5. **The objects inside are not made thread-safe:** a `List` inside a `ConcurrentDictionary` is still a plain `List`.
6. **Forgetting `Writer.Complete()` / `CompleteAdding()`:** the consumer waits forever.

## 🎯 Interview Questions

1. **Why do concurrent collections exist? Isn't `lock` enough?**
   `lock` works, but it locks the whole collection behind one door. Concurrent collections use finer-grained locks (lock striping) or lock-free structures, so they scale better. They also come with atomic methods like `GetOrAdd`.
2. **Is `GetOrAdd` thread-safe?**
   The add is thread-safe, but the value factory can run more than once. If it is expensive, wrap the value in `Lazy<T>`.
3. **What is the difference between `ConcurrentBag` and `ConcurrentQueue`?**
   The queue keeps FIFO order. The bag has no order and is fastest when the same thread both adds and takes.
4. **How do you build a producer-consumer?**
   `BlockingCollection` in synchronous code, `Channel<T>` in async code.
5. **What is the difference between `BlockingCollection` and `Channel`?**
   `BlockingCollection` blocks the thread while waiting. `Channel` waits with `await`, so the thread is free. Channels are also faster.
6. **Bounded vs unbounded channel?**
   Unbounded grows without limit (memory risk). Bounded makes the writer wait when full (backpressure), or drops items based on `FullMode`.
7. **What is backpressure?**
   The slow consumer slowing down the fast producer, so the system doesn't overflow.
8. **`ImmutableDictionary` vs `ConcurrentDictionary`?**
   Immutable never changes; every change creates a new copy. Good when reads are frequent and writes are rare. Concurrent is changed in place and suits frequent writes.
9. **Is a plain `Dictionary` thread-safe if we only read from it?**
   Yes. As long as nobody writes, many threads can read it safely. Problems start with writes.

## ✍️ Try It Yourself (Exercises)

- [ ] **DictionaryBasics:** Count word frequencies of a long text with `Parallel.ForEach` and `AddOrUpdate`. Compare with a plain `Dictionary` + `lock`.
- [ ] **GetOrAddTrap:** Change `ThreadCount` to 50. How many database calls do you get without `Lazy`?
- [ ] **QueueStackBag:** Build a simple object pool (`Rent` / `Return`) with `ConcurrentBag<StringBuilder>`.
- [ ] **ChannelBasics:** Remove `channel.Writer.Complete()`. What happens? Why?
- [ ] **BoundedChannel:** Change the capacity from 2 to 10. How does the waiter's timing change?
- [ ] **New example:** Build a 3-stage pipeline with two channels: read orders → cook → serve.

## 🔜 Next Topics

- `SemaphoreSlim` (at most N operations at the same time)
- `CancellationToken` (cancelling work, also in `ReadAllAsync`)
- `BackgroundService` + Channel as a job queue in ASP.NET Core
- Immutable collections (`ImmutableList`, `ImmutableDictionary`)
