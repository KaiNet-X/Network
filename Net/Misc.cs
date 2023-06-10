using Net.Serialization;
using System;
using System.Threading.Tasks;

namespace Net;

/// <summary>
/// State of the connection
/// </summary>
public enum ConnectState
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