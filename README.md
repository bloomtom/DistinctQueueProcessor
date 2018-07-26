# Distinct Queue Processor
>The indexed multi-thread queue.

This library was built to manage a large queue of long running parallel processes. Parallelization is easily configured and threading is managed internally. The queue is indexed in a library for fast lookups of running tasks, and to provide the ability to reject items which are already in queue.

## Nuget Packages

Package Name | Target Framework | Version
---|---|---
[DistinctQueueProcessor](https://www.nuget.org/packages/DistinctQueueProcessor) | .NET Standard 2.0 | ![NuGet](https://img.shields.io/nuget/v/DistinctQueueProcessor.svg)

## Usage
The main contents of this library are in a single abstract class, `DQP.DistinctQueueProcessor<T>`. Create your own derivative class which overrides the following required methods:
 - `Process(T item)`
   - Do your work here. This method is run on a threaded task. 
 - `Error(T item, Exception ex)`
   - Called when an exception is caught from `Process(T item)`

#### Usage Example
```csharp
class DqpExample : DistinctQueueProcessor<string>
{
    protected override void Error(string item, Exception ex)
    {
        throw ex;
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
