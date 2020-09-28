using System;
using System.Threading.Tasks;

namespace NationalInstruments.Tools.TestUtilities
{
    public static class TimeoutHelper
    {
        public static async Task AssertNoTimeoutAsync(Task task, TimeSpan timeSpan)
        {
            var timeoutTask = Task.Delay(timeSpan);
            if (timeoutTask == await Task.WhenAny(task, timeoutTask).ConfigureAwait(false))
            {
                throw new TimeoutException("The test timed out");
            }

            if (task.IsFaulted)
            {
                throw task.Exception;
            }
        }

        public static Task AssertNoTimeoutAsync(Func<Task> func, TimeSpan timeSpan)
        {
            return AssertNoTimeoutAsync(func(), timeSpan);
        }
    }
}
