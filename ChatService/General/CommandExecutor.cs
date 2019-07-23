using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatService
{
    public class ArgKeyValuePair
    {
        public string key;
        public string value;
    }
    public class Command
    {
        public string command;
        public string description;

        public string[] availableArguments;

        public Action<ArgKeyValuePair[]> callback;
    }

    public class CommandExecutor
    {
        public const char COMMANDS_SEPARATOR = '-';

        private Dictionary<string, Command> shellCommands;

        public CommandExecutor()
        {
            shellCommands = new Dictionary<string, Command>();
        }

        public void Add(string key, string[] availableArgs, string description, Action<ArgKeyValuePair[]> callback)
        {
            shellCommands[key] = new Command()
            {
                command = key,
                availableArguments = availableArgs,
                description = description,
                callback = callback
            };
        }
        public void Remove(string key)
        {
            if (shellCommands.ContainsKey(key))
            {
                shellCommands.Remove(key);
            }
        }

        /// <summary>
        /// This allows you to execute custom commands in the server
        /// </summary>
        /// <param name="scheduler">If null the task will be executed in the caller thread. If is a valid instance will be scheduled</param>
        /// <returns>Return true if the command is executed. False if not</returns>
        public bool Execute(string fullCommandLine, TaskScheduler scheduler)
        {
            string[] commands = fullCommandLine.Split(COMMANDS_SEPARATOR);

            int firstSpaceIdx = fullCommandLine.IndexOf(" ");

            string cmd = "";
            if (firstSpaceIdx != -1)
                cmd = fullCommandLine.Substring(0, firstSpaceIdx);    //the actual key of the command
            else
                cmd = fullCommandLine;  //no args

            //string argsString = fullCommandLine.Substring(firstSpaceIdx + 1, fullCommandLine.Length - firstSpaceIdx -1);

            if (!this.shellCommands.ContainsKey(cmd))
                return false;

            List<ArgKeyValuePair> args = new List<ArgKeyValuePair>();

            for (int i = 0; i < commands.Length; i++)
            {
                if (i == 0)     //ignore the base command
                    continue;

                //get first word 
                int firstSpace = commands[i].IndexOf(' ');
                string argKey = commands[i].Substring(0, firstSpace);
                string value = commands[i].Substring(firstSpace + 1, commands[i].Length - firstSpace - 1);

                if (i != commands.Length - 1)
                    value = value.Substring(0, value.Length-1);

                var kvPair = new ArgKeyValuePair()
                {
                    key = argKey,
                    value = value
                };
                args.Add(kvPair);
            }

            if (scheduler != null)
                scheduler.Schedule(() => shellCommands[cmd].callback(args.ToArray()));
            else
                shellCommands[cmd].callback(args.ToArray());

            return true;
            //string[] splittedCommand = command.Split(' ');

            //string finalCommand = splittedCommand[0];
            //string[] args = (from a in splittedCommand where a != finalCommand select a).ToArray();

            //if (!shellCommands.ContainsKey(finalCommand))
            //    return false;

            //if (scheduler != null)
            //    scheduler.Schedule(shellCommands[command], args);
            //else
            //    shellCommands[command](args);

            //return true;
        }


        public List<string> GetAllAvailableCommands()
        {
            return (from c in shellCommands select c.Key).ToList();
        }
    }
}
