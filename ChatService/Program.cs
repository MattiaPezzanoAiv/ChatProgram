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
            Console.SetWindowSize(100, 10);

            ChatLog.AddChannel(new ChatLogConsoleChannel());
            ChatLog.Info("Server running.");
            ChatServer server = new ChatServer();

            //server.CommandExecutor.Add("print_hello", () => ChatLog.Info("Hello")); //test

            ////print all the active rooms
            //server.CommandExecutor.Add("print rooms", () => {
            //    int i = 0;
            //    foreach (var room in server.GetActiveRooms())
            //    {
            //        ChatLog.Info(string.Format("{0}) Name -> {1} --- Host -> {2}", i, room.Name, room.Host));
            //        i++;
            //    }
            //});

            ////specific room members
            //server.CommandExecutor.Add("members", () => {

            //    ChatLog.Info("type room name:");
            //    string room = Console.ReadLine();
            //    if (!server.RoomExistsByName(room))
            //    {
            //        ChatLog.Error("room does not exists");
            //        return;
            //    }

            //    int i = 0;
            //    foreach (var members in server.GetRoomMembers(room))
            //    {
            //        ChatLog.Info(string.Format("---> " + members));
            //        i++;
            //    }
            //});

            server.Start(); //implicit args
        }
    }
}
