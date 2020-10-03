# Namespaces

## When to create a new namespace

A new namespace should be added when new types are being added that have an architectural distinction from the other types in the system. If you are introducing a new feature area to the system it should probably go into a new namespace. If types should have an architectural relationship that should be expressed a on a layer diagram (type(s) A can / cannot depend on type(s) B) then you should create a new namespace. 

## When not to create a new namespace

Namespaces are not to help you as the developer organize the files in the solution in the project explorer. Do not add folders / namespaces unless they are creating a real separation of functionality. Adding a namespace should require to update Architecture Diagrams to define what is allowed to use and not use the new namespace and what your new namespace is allowed to use and not use. In addition to looking at what your namespace references, also look at who references your namespace. If all or at least most of the clients of the namespace need both namespaces, then you shouldn't create a separate namespace. If your new namespace has the same rules as the parent namespace then it probably should not exist. 

# Writing exception-safe code

Treat every line of code as having the potential to throw an exception. Code should be written such that exceptions cleanly abort the current operation, leaving all data structures in a consistent state that is balanced with respect to paired operations (e.g. init/cleanup).

When possible, defer connecting new data structures to existing data structures until the new structures are fully constructed. When necessary, use try/finally, try/catch/throw, or using/dispose to back out incomplete operations, to release resources deterministically when required, or to ensure that paired operations remain balanced. Consider implementing `IDisposable` when it would enable client code to be written more simply via the using statement.
This guideline can be bypassed in specific cases when it requires complexity that is not worthwhile given the specific consequences and probability of failure. These cases should be justified in code comments, or in this document if the situation is sufficiently common.

**Rethrowing Exceptions**

When re-throwing the same exception object you have caught, use the general throw; statement rather than explicitly throwing the reference you caught. Re-throwing a reference explicitly causes the stack trace to reset. For example:

```csharp
try  
{  
    // Do work
}
catch (MyExceptionType1 e)
{
   throw e; // Incorrect
}
 
try 
{
    // Do work
}
catch (MyExceptionType2 e)
{
   throw; // Correct
}
```

**Catching Exceptions**

Catch in moderation. Generally, the only places a catch without a rethrow are needed are at:

- A low level to catch a specific exception. 
- When an API is poorly designed or buggy, there may be no way to avoid an exception in a known circumstance.
- At a very high level to catch any exception. For example, an application might surround its handling of user input with a try/catch in order to report any errors that occur during user initiated operations. However, this would be a poor behavior within in a component such as a control because it would prevent the application from controlling how errors are presented.

Microsoft's Exception Guidelines can be found [here](http://msdn.microsoft.com/en-us/library/ms229014(VS.80).aspx). The importance and relevance of these guidelines vary, but they are worth reading.

# Events

When defining an event that a C# class can raise, follow these steps.
1. Inherit from `System.EventArgs` for the event parameter
The parameter passed to the event should inherit from `EventArgs`, and should in general be a data class with a bunch of properties:

```csharp
using System;
 
public class MyEventArgs : EventArgs
{
    public int SomeValue { get; set; }
    public string SomeOtherValue { get; set; }
}
```
2. Define the event handler
```csharp
public class HandleMyEvent
{
    public event EventHandler<MyEventArgs> SomethingCool;
}
```
3. Define an OnEvent method
   - The class that exposes the event should also define a protected, virtual method that raises it.  This serves 2 purposes:
   1. It's a place to centralize the null-check to cover the case where no event handlers are registered.
   2. It allows sub-classes to add or override the behavior of the event (events are otherwise non-polymorphic...ain't no such thing as a virtual event).

```csharp
public class MyEventHandlerClass
{
    public event EventHandler<MyEventArgs> SomethingCool;
  
    protected virtual void OnSomethingCool(MyEventArgs e)
    {
        // Take a snapshot to avoid a race condition between the null check and the trigger
        EventHandler<MyEventArgs> temp = SomethingCool;
  
        if (temp != null)
        {
            temp(this, e);
        }
    }
}
```

## Multi-threaded events
The above examples only work for events that are registered, unregistered, and triggered from a single thread.  If you plan to use events in a multi-threaded context, you must:
- Ask whether a multi-threaded event is really necessary.  If not, don't do it!
- Decide on the threading semantics for your event
- Is it okay to have multiple handlers going off at once, or do I want to serialize them?
- Is it okay for a client to receive an event after he's unregistered the handler?
- Is it okay for a client to register or unregister an event handler from within an event handler?
- Define custom add/remove methods for the event handler.
- Implement the OnEvent() function such that any threading policies you desire are implemented.
- Have someone that has good knowledge of events and threading review your code.

Here is an example of a way that a multi-threaded event might be implemented.  It has the following rules:
- Events can be registered, unregistered, and triggered from any thread.
- Event handlers will be serialized (no event handlers will be executed concurrently).
- Events will never be triggered on clients after they unregister a handler.
- Clients must not register or unregister an event from within an event handler, or a deadlock may result.

```csharp
public class ClassContainingThreadSafeEvents
{
   private readonly Object _eventLock = new Object();
 
   // Note: no "event" keyword
   private EventHandler<MyEventArgs> _internalHandler;
 
   public event EventHandler<MyEventArgs> SomeEvent
   {
      add
      {
         lock(_eventLock) { _internalHandler += value; }
      }
      remove
      {
         lock(_eventLock) { _internalHandler -= value; }
      }
   }
 
   protected virtual void OnSomeEvent(MyEventArgs e)
   {
      lock(_eventLock)
      {
         if(_internalHandler != null)
         {
            _internalHandler(this, e);
         }
      }
    }
}
```