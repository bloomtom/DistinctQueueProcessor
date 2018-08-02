# Distinct Queue Processor
>The indexed multi-thread queue.

This library was built to manage a large queue of long running parallel processes. Parallelization is easily configured and threading is managed internally. The queue is indexed in a library for fast lookups of running tasks, and to provide the ability to reject items which are already in queue.

## Nuget Packages

Package Name | Target Framework | Version
---|---|---
[DistinctQueueProcessor](https://www.nuget.org/packages/bloomtom.DistinctQueueProcessor) | .NET Standard 2.0 | ![NuGet](https://img.shields.io/nuget/v/bloomtom.DistinctQueueProcessor.svg)

## Usage
The main contents of this library are in a single abstract class, `DQP.DistinctQueueProcessor<T>`. Create your own derivative class which overrides the following required methods:
 - `Process(T item)`
   - Do your work here. This method is run on a threaded task. 
 - `Error(T item, Exception ex)`
   - Called when an exception is caught from `Process(T item)`. Throwing an exception here will kill the running worker so take care.

Also provided is an wrapper which takes Actions in the constructor. See the examples below for more detail.

#### Inheritance Example
Create a new class inheriting from `DistinctQueueProcessor<T>` where `T` is the type of object you want to enqueue.
```csharp
class DqpExample : DistinctQueueProcessor<string>
{
    protected override void Error(string item, Exception ex)
    {
        Console.Error.WriteLine(ex.ToString());
    }

    protected override void Process(string item)
    {
        // This will be run on a thread, so don't be surprised if messages print out-of-order.
        Console.WriteLine(item);
    }
}
```
You'd then use your class as follows:
```csharp
var example = new DqpExample();
example.AddItem("Hello, world!");
```
#### Action Example

If inheriting from `DistinctQueueProcessor` in a custom class is too heavy for your use case, `ActionQueue` can be used instead. It's a simple wrapper around the base class which takes two action as constructor parameters.

```csharp
var actionQueue = new ActionQueue<string>(
	new Action<string>(x =>
	{
		Console.WriteLine(item);
	}),
	new Action<string, Exception>((x, ex) =>
	{
		Console.Error.WriteLine(ex.ToString());
	}));
    
actionQueue.AddItem("Hello, world!");
```

## Gotchas

Internally the queue is indexed using a `Dictionary<string, T>`, where the key is `T.ToString()`. Ensure your `T` has a `ToString` implementation which returns short unique values.