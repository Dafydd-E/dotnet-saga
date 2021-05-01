using System;
using System.Threading.Tasks;

namespace Saga
{
    public interface IScheduler
    {
        Task Schedule(Func<Task> action, int delayMilliseconds);
        Task Wait(int waitFor);
    }
}