using System;
using System.Threading.Tasks;

namespace Saga
{
    public class DefaultScheduler : IScheduler
    {
        public async Task Schedule(Func<Task> action, int delayMilliseconds)
        {
            await Task.Delay(delayMilliseconds);
            await action.Invoke();
        }

        public async Task Wait(int delayMilliseconds)
        {
            await Task.Delay(delayMilliseconds);
        }
    }
}