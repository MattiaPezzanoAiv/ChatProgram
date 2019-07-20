using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatService
{
    /// <summary>
    /// This class is supposed to be updated in the main thread so other threads can execute custom code in the main.
    /// But can be used also in the opposite way or to execute code from thread to thread.
    /// In unity you could create a monobehaviour and update this script (race conditions are not managed by this class)
    /// </summary>
    public class TaskScheduler
    {
        private Queue<Action> tasks;

        private object _lock;

        public TaskScheduler()
        {
            _lock = new object();
        }

        public void Schedule(Action action)
        {
            lock(_lock)
            {
                if (tasks == null)
                    tasks = new Queue<Action>();

                tasks.Enqueue(action);
            }
        }

        /// <summary>
        /// Call this in the main thread to execute scheduled tasks
        /// </summary>
        public void Update()
        {
            if (tasks == null)
                return;

            if (tasks.Count <= 0)
                return;

            var task = tasks.Dequeue();
            while(task != null)
            {
                task.Invoke();
                task = null;

                if (tasks.Count > 0)
                    task = tasks.Dequeue();    
            }
        }
    }
}
