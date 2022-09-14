namespace Net;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class Invoker
{
    private List<Action> InvokationList = new();
    private SemaphoreSlim _semaphore = new(1, 1);
    private Task runner;

    public void AddAction(Action t)
    {
        Utilities.ConcurrentAccess(() =>
        {
            InvokationList.Add(t);
        }, _semaphore);

        if (runner == null || runner.IsCompleted)
            runner = Task.Run(async () => await InvokeTasks());
    }

    private async Task InvokeTasks()
    {
        List<Action> actions = new List<Action>();
        while (true)
        {
            if (InvokationList.Count == 0)
                await Task.Delay(1);

            await Utilities.ConcurrentAccessAsync((ct) =>
            {
                if (InvokationList.Count == 0)
                {
                    return Task.CompletedTask;
                }
                actions = new List<Action>(InvokationList);
                InvokationList.Clear();
                return Task.CompletedTask;
            }, _semaphore);

            foreach (var a in actions)
                try
                {
                    a();
                }
                catch
                {

                }

            actions.Clear();
        }
    }
}
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
