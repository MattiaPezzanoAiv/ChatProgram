using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using ChatService;
using ChatService.Log;
using ChatService.Client;

namespace MessageSender
{
    class Program
    {
        static void Main(string[] _args)
        {
            ChatLog.AddChannel(new ChatLogConsoleChannel());
            bool running = true;

            ChatLog.Info("type name:");
            string name = Console.ReadLine();

            ChatClient client = new ChatClient("127.0.0.1", 2001, name, true);
            bool connect = false;

            //set the client name
            client.CommandExecutor.Add("setname", new string[] { "-name" }, "Allow to change the name of the client if it's not joined", (args) =>
            {

                if (args.Length != 1)
                {
                    ChatLog.Error("Only argument number accepted is 1");
                }
                else
                {
                    bool success = client.ChangeClientName(args[0].value);
                    if (!success)
                        ChatLog.Warning("Operation not allowed when the client is joined");
                }
            });

            //connect to server
            client.CommandExecutor.Add("connect", new string[] { "-join" }, "Connect to the server", (args) =>
            {

                if (args.Length == 1)
                {
                    if (args[0].value == "true")
                        connect = client.Connect(true);
                    else if (args[0].value == "false")
                        connect = client.Connect(false);
                    else
                        ChatLog.Error("Argument not valid");
                }
                else if (args.Length == 0)
                {
                    connect = client.Connect(false);
                }
                else
                {
                    ChatLog.Error("Arguments number error. Should type join if you want autojoin or leave just connect command");
                }
            });

            //client.CommandExecutor.Execute("setname -name pippo", null);
            //just join the server
            client.CommandExecutor.Add("join", new string[] { }, "Join the server", (args) =>
            {
                client.Join();
            });

            //just quit the server
            client.CommandExecutor.Add("quit", new string[] { }, "Quit the server", (args) =>
            {
                client.Quit();
            });

            //ask for the joined clients
            client.CommandExecutor.Add("askjoined", new string[] { }, "Ask the server for the connected clients", (args) =>
           {
               client.AskForConnectedUsers();
           });

            //message a specific client
            client.CommandExecutor.Add("message", new string[] {"-text","-dest" }, "Message a specific client specified with -dest, the message must be specified with -text argument", (args) =>
            {
                if(args.Length != 2)
                {
                    ChatLog.Error("Only argument number accepted is 2");
                    return;
                }

                string dest  = "";
                string message = "";

                foreach (var arg in args)
                {
                    if (arg.key == "dest")
                        dest = arg.value;
                    else
                        message = arg.value;
                }
                client.SendMessage(dest, message, false);
            });


            //message a specific room
            client.CommandExecutor.Add("messageroom", new string[] { "-text", "-dest" }, "Message a specific room specified with -dest, the message must be specified with -text argument", (args) =>
            {
                if (args.Length != 2)
                {
                    ChatLog.Error("Only argument number accepted is 2");
                    return;
                }

                string dest = "";
                string message = "";

                foreach (var arg in args)
                {
                    if (arg.key == "dest")
                        dest = arg.value;
                    else
                        message = arg.value;
                }
                client.SendMessage(dest, message, true);
            });

            //message a specific room
            client.CommandExecutor.Add("createroom", new string[] { "-name" }, "create a new room with a specified name with -name argument", (args) =>
            {
                if (args.Length != 1)
                {
                    ChatLog.Error("Only argument number accepted is 1");
                    return;
                }

                client.CreateRoom(args[0].value);
            });

            client.CommandExecutor.Add("closeroom", new string[] { "-name" }, "close the room (if you're the host) with a specified name with -name argument", (args) =>
            {
                if (args.Length != 1)
                {
                    ChatLog.Error("Only argument number accepted is 1");
                    return;
                }

                client.CloseRoom(args[0].value);
            });

            client.CommandExecutor.Add("members", new string[] { "-name" }, "locally print the room members", (args) =>
            {
                if (args.Length != 1)
                {
                    ChatLog.Error("Only argument number accepted is 1");
                    return;
                }

                if (client.JoinedRooms.ContainsKey(args[0].value))
                {
                    foreach (var m in client.JoinedRooms[args[0].value].Members)
                    {
                        ChatLog.Info(m);
                    }
                }
                else
                    ChatLog.Warning("Room not joined");
            });

            client.CommandExecutor.Add("invite", new string[] { "-name", "-room" }, "invite a user to a room", (args) =>
            {
                if (args.Length != 2)
                {
                    ChatLog.Error("Only argument number accepted is 2");
                    return;
                }

                string user = "";
                string room = "";

                foreach (var arg in args)
                {
                    if (arg.key == "name")
                        user = arg.value;
                    else
                        room = arg.value;
                }
                client.Invite(room, user);
            });

            client.CommandExecutor.Add("leave", new string[] { "-room" }, "leave the room", (args) =>
            {
                if (args.Length != 1)
                {
                    ChatLog.Error("Only argument number accepted is 1");
                    return;
                }

                client.LeaveRoom(args[0].value);
            });
            //finish commands implementation

            client.StartCommandsThread();

            while (running)
            {
                client.UpdateScheduler();
            }
        }
    }
}
