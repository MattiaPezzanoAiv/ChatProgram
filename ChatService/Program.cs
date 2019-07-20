using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatService.Packets;
using ChatService.Log;
using ChatService.Server;

namespace ChatService
{
    class Program
    {
        static void Main(string[] args)
        {
            ChatLog.AddChannel(new ChatLogConsoleChannel());
            ChatLog.Info("Server running.");
            ChatServer server = new ChatServer();

            server.CommandExecutor.Add("print_hello", () => ChatLog.Info("Hello"));

            server.Start(); //implicit args
        }
    }
}
