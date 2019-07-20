using System;
using System.Collections.Generic;

namespace ChatService.Log
{
    /// <summary>
    /// This interface is responsible to deliver the log to the desired channel.
    /// ex. in a c# console app the interface should use ChatLog.Info method (default implementation)
    /// ex. in unity should use debug log method
    /// </summary>
    public interface IChatLogChannel
    {
        void Info(string message);
        void Warning(string message);
        void Error(string message);

        bool IsActive { get; set; }
    }
    public class ChatLogConsoleChannel : IChatLogChannel
    {
        public bool IsActive { get; set; } = true;

        public void Error(string message)
        {
            Console.WriteLine("Error -> " + message);
        }

        public void Info(string message)
        {
            Console.WriteLine("Info -> " + message);
        }

        public void Warning(string message)
        {
            Console.WriteLine("Warning -> " + message);
        }
    }


    public static class ChatLog
    {
        private static List<IChatLogChannel> channels;
        private static object _lock;

        static ChatLog()
        {
            _lock = new object();
        }

        public static void AddChannel(IChatLogChannel channel)
        {
            if (channels == null)
                channels = new List<IChatLogChannel>();
            channels.Add(channel);
        }
        public static void RemoveChannel(IChatLogChannel channel)
        {
            if (channels.Contains(channel))
                channels.Remove(channel);
        }
        
        public static void Info(string message)
        {
            lock(_lock)
            {
                foreach (var c in channels)
                {
                    c.Info(message);
                }
            }
        }
        public static void Warning(string message)
        {
            foreach (var c in channels)
            {
                c.Warning(message);
            }
        }
        public static void Error(string message)
        {
            foreach (var c in channels)
            {
                c.Error(message);
            }
        }
    }
}
