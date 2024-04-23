namespace Net;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

internal static class Utilities
{
    public static int IndexInByteArray(byte[] Bytes, byte[] SearchBytes, int offset = 0)
    {
        for (int i = offset; i <= Bytes.Length - SearchBytes.Length; i++)
            for (int I = 0; I < SearchBytes.Length; I++)
                if (!SearchBytes[I].Equals(Bytes[i + I]))
                    break;
                else if (I == SearchBytes.Length - 1 && SearchBytes[I].Equals(Bytes[i + I]))
                    return i;
        return -1;
    }

    public static int IndexInByteArray(List<byte> bytes, byte[] SearchBytes, int offset = 0)
    {
        for (int i = offset; i <= bytes.Count - SearchBytes.Length; i++)
            for (int I = 0; I < SearchBytes.Length; I++)
                if (!SearchBytes[I].Equals(bytes[i + I]))
                    break;
                else if (I == SearchBytes.Length - 1 && SearchBytes[I].Equals(bytes[i + I]))
                    return i;
        return -1;
    }

    public static int IndexInByteSpan(ReadOnlySpan<byte> bytes, Span<byte> SearchBytes, int offset = 0)
    {
        for (int i = offset; i <= bytes.Length - SearchBytes.Length; i++)
            for (int I = 0; I < SearchBytes.Length; I++)
                if (!SearchBytes[I].Equals(bytes[i + I]))
                    break;
                else if (I == SearchBytes.Length - 1 && SearchBytes[I].Equals(bytes[i + I]))
                    return i;
        return -1;
    }

    public static void ConcurrentAccess(Action a, SemaphoreSlim s)
    {
        s.Wait();

        try
        {
            a();
        }
        finally
        {
            s?.Release();
        }
    }

    public static void Lock(this SemaphoreSlim s, Action a) =>
        ConcurrentAccess(a, s);

    public static async Task ConcurrentAccessAsync(Func<CancellationToken, Task> a, SemaphoreSlim s, int timeout = 2500)
    {
        using CancellationTokenSource cts = new CancellationTokenSource();

        cts.CancelAfter(timeout);

        await s.WaitAsync();

        try
        {
            await a(cts.Token);
        }
        finally
        {
            s?.Release();
        }
    }

    public static async Task ConcurrentAccessAsync(Func<Task> a, SemaphoreSlim s)
    {
        await s.WaitAsync();

        try
        {
            await a();
        }
        finally
        {
            s?.Release();
        }
    }

    public static Task LockAsync(this SemaphoreSlim s, Func<Task> a) =>
        ConcurrentAccessAsync(a, s);

    public static bool MatchAny<T>(T original, params T[] matches) => MatchAny<T>(original, matches as IEnumerable<T>);

    public static bool MatchAny<T>(T original, IEnumerable<T> matches)
    {
        foreach (var match in matches)
            if (original.Equals(match))
                return true;
        return false;
    }

    public static bool TryDequeueRange<T>(this System.Collections.Concurrent.ConcurrentQueue<T> queue, out T[] result)
    {
        result = new T[queue.Count];

        for (int i = 0; i < result.Length; i++)
            if (queue.TryDequeue(out T item))
                result[i] = item;
            else if (i == 0)
                return false;
            else
                result = result[..(i - 1)];
        return true;
    }

    public static void EnqueueRange<T>(this System.Collections.Concurrent.ConcurrentQueue<T> queue, IEnumerable<T> range)
    {
        foreach (T r in range)
            queue.Enqueue(r);
    }

    public static Func<T, Task> SyncToAsync<T>(Action<T> action) => a =>
    {
        action(a);
        return Task.CompletedTask;
    };

    public static Func<T1, T2, Task> SyncToAsync<T1, T2>(Action<T1, T2> action) => (a, b) =>
    {
        action(a, b);
        return Task.CompletedTask;
    };
}