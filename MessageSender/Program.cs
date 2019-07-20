﻿using System;
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
        static void Main(string[] args)
        {
            ChatLog.AddChannel(new ChatLogConsoleChannel());
            bool running = true;

            ChatLog.Info("type name:");
            string name = Console.ReadLine();

            ChatClient client = new ChatClient("127.0.0.1", 2001, name, true);

            //should implemnet the command executor like in server

            new System.Threading.Thread(() => {
                while (true)
                    client.UpdateScheduler();
            }).Start();

            while (running)
            {
                ChatLog.Info("Type command: ");
                string input = Console.ReadLine();
                bool connect = false;

                switch (input)
                {
                    case "name":
                        name = Console.ReadLine();
                        client.ChangeClientName(name);
                        break;
                    case "connect":
                        connect = client.Connect(true);
                        break;
                    case "join":
                        client.Join();
                        break;
                    case "quit":
                        {
                            client.Quit();
                            break;
                        }
                    case "message":
                        {
                            ChatLog.Info("     type destination user:");
                            string user = Console.ReadLine();
                            ChatLog.Info("     type message:");
                            string message = Console.ReadLine();
                            client.SendMessage(user, message);
                            break;
                        }
                    case "ask_joined":
                        {
                            client.AskForConnectedUsers();
                            break;
                        }
                    default:
                        ChatLog.Info("   ***Invalid command");
                        break;
                }
            }
        }
    }
}