namespace Net;

using Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// State of the connection
/// </summary>
public enum ConnectionState
{
    NONE,
    PENDING,
    CONNECTED,
    CLOSED
}

internal interface IInvokable
{ 
    void Invoke(object o);
}

internal class Invokable<T> : IInvokable
{
    public T Value { get; set; }
    private readonly Action<T> _action;

    public Invokable(Action<T> action)
    {
        _action = action;
    }

    public void Invoke(object o) =>
        _action((T)o);
}
internal class Invokable : IInvokable
{
    public void Invoke(object o)
    {
        throw new NotImplementedException();
    }
}
internal interface IAsyncInvokable
{
    Task InvokeAsync(object o);
}

internal class AsyncInvokable<T> : IAsyncInvokable
{
    public T Value { get; set; }
    private readonly Func<T, Task> _action;

    public AsyncInvokable(Func<T, Task> action)
    {
        _action = action;
    }

    public Task InvokeAsync(object o) =>
        _action((T)o);
}

/// <summary>
/// Default values or constants the library uses
/// </summary>
public static class Consts
{
    /// <summary>
    /// Default serializer is the MpSerializer
    /// </summary>
    public static ISerializer DefaultSerializer = MpSerializer.Instance;
}

/// <summary>
/// Information about the disconnection
/// </summary>
public class DisconnectionInfo
{
    /// <summary>
    /// The exception that was thrown, if applicable
    /// </summary>
    public Exception Exception { get; set; }

    /// <summary>
    /// The reason the connection closed
    /// </summary>
    public DisconnectionReason Reason { get; set; }
}

/// <summary>
/// Represents the reason the connection was closed
/// </summary>
public enum DisconnectionReason
{
    /// <summary>
    /// The connection was closed by the remote host
    /// </summary>
    Closed,
    /// <summary>
    /// The connection was forced to close due to an error
    /// </summary>
    Aborted,
    /// <summary>
    /// The connection timed out
    /// </summary>
    TimedOut
}

/// <summary>
/// Provides a readonly view over a list.
/// </summary>
/// <typeparam name="T">List type</typeparam>
public class GuardedList<T> : IEnumerable<T>
{
    private readonly List<T> _list;

    public T this[int index] => _list[index];

    public int Count => _list.Count;

    /// <summary>
    /// Creates a guarded list over a list
    /// </summary>
    /// <param name="list"></param>
    public GuardedList(List<T> list)
    {
        _list = list;
    }

    public bool Contains(T item) => 
        _list.Contains(item);

    public IEnumerator<T> GetEnumerator() =>
        ((IEnumerable<T>)_list).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        ((IEnumerable)_list).GetEnumerator();
    
    public static implicit operator GuardedList<T>(List<T> obj) => new GuardedList<T>(obj);
}