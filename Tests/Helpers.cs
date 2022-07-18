using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests;

internal static class Helpers
{
    internal static async Task<bool> WaitForCondition(Func<bool> condition, int timeoutMs)
    {
        var conditionTask = Task.Run(async () =>
        {
            while (!condition())
                await Task.Delay(10);
        });
        var timeoutTask = Task.Delay(timeoutMs);
        if (await Task.WhenAny(conditionTask, timeoutTask) == conditionTask)
            return true;
        return false;
    }
}
