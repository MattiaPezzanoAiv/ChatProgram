using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatService
{
    public class CommandExecutor
    {
        private Dictionary<string, Action> shellCommands;

        public CommandExecutor()
        {
            shellCommands = new Dictionary<string, Action>();
        }

        public void Add(string key, Action command)
        {
            shellCommands[key] = command;
        }
        public void Remove(string key)
        {
            if(shellCommands.ContainsKey(key))
            {
                shellCommands.Remove(key);
            }
        }

        /// <summary>
        /// This allows you to execute custom commands in the server
        /// </summary>
        /// <param name="scheduler">If null the task will be executed in the caller thread. If is a valid instance will be scheduled</param>
        /// <returns>Return true if the command is executed. False if not</returns>
        public bool Execute(string command, TaskScheduler scheduler)
        {
            if (!shellCommands.ContainsKey(command))
                return false;

            if (scheduler != null)
                scheduler.Schedule(shellCommands[command]);
            else
                shellCommands[command]();

            return true;
        }
    }
}
