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

public class DisconnectionInfo
{
    public Exception Exception { get; set; }
    public string Reason { get; set; }
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