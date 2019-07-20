using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Net.Sockets;
using ChatService.Log;
using ChatService.Packets;

namespace ChatService.Server
{
    public sealed class ChatServer : ChatNetworkObject
    {
        private bool exit = false;

        private PacketMap<Packet, ChatClientData> packetsSupportedMap;

        //implement command executor in client
        //try to generalize the shared fields

        private List<ChatClientData> clientsConnected;
        private Dictionary<Socket, ChatClientData> clientsConnectedDic;

        private List<Socket> readCheck, writeCheck, errorCheck;

        public ChatServer()
        {
            scheduler = new TaskScheduler();
            tcpLayer = new ChatTcpLayer(false, scheduler, PacketUtilities.PACKET_SIZE);

            readCheck = new List<Socket>();
            writeCheck = new List<Socket>();
            errorCheck = new List<Socket>();

            clientsConnected = new List<ChatClientData>();
            clientsConnectedDic = new Dictionary<Socket, ChatClientData>();

            CommandExecutor = new CommandExecutor();

            packetsSupportedMap = new PacketMap<Packet, ChatClientData>();
            packetsSupportedMap[Protocol.ASK_ALL_CONNECTED] = this.AskAllConnected;
            packetsSupportedMap[Protocol.MESSAGE_SENT] = this.MessageSent;
            packetsSupportedMap[Protocol.QUIT] = this.QuitReceived;
            packetsSupportedMap[Protocol.JOIN] = this.JoinReceived;
        }


        #region INTERNAL_USE
        /// <summary>
        /// This is used to build a list for socket selecting
        /// </summary>
        void BuildReadableClientsList()
        {
            readCheck.Clear();
            readCheck = (from client in clientsConnected select client.Socket).ToList();
        }
        void AddConnectedClient(ChatClientData client)
        {
            clientsConnected.Add(client);
            clientsConnectedDic.Add(client.Socket, client);

            ChatLog.Info("** New client connected -> " + client.Socket.RemoteEndPoint.ToString());
        }
        void RemoveConnectedClient(ChatClientData client, bool closeSocket = true)
        {
            if (closeSocket)
                client.Socket.Close();

            clientsConnected.Remove(client);
            clientsConnectedDic.Remove(client.Socket);
        }
        void SendMessageToAllJoined(byte[] bytes)
        {
            List<Socket> sockets = (from client in clientsConnected where client.HasJoined select client.Socket).ToList();
            foreach (var socket in sockets)
            {
                try
                {
                    socket.Send(bytes);
                }
                catch
                {
                    continue;
                }
            }
        }
        /// <summary>
        /// Returns true if a client is JOINED with the given name
        /// </summary>
        /// <param name="name"></param>
        bool ClientExists(string name)
        {
            foreach (var client in clientsConnected)
            {
                if (client.Name == name)
                    return true;
            }
            return false;
        }/// <summary>
         /// 
         /// </summary>
         /// <param name="name"></param>
         /// <returns>Returns a ChatClientData object if there is a client JOINED with the given name</returns>
        ChatClientData GetClientByName(string name)
        {
            foreach (var client in clientsConnected)
            {
                if (client.Name == name)
                    return client;
            }
            return null;
        }
        #endregion


        /// <summary>
        /// Start the server systems and threads
        /// </summary>
        public void Start(string bindingAddress = "127.0.0.1", int port = 2001)
        {
            //register callback
            tcpLayer.onIncomingConnectionAccepted += (socket) =>
            {
                ChatLog.Info("connection added");
                ChatClientData client = new ChatClientData()
                {
                    Socket = socket,
                    ReceiveBuffer = new byte[PacketUtilities.PACKET_SIZE],
                    HasJoined = false,
                    Name = null
                };

                AddConnectedClient(client);
            };

            tcpLayer.Bind(bindingAddress, port);
            tcpLayer.StartListen();

            //start receiving thread
            var receivingThread = new Thread(() =>
            {
                while (!exit)
                {
                    tcpLayer.Accept();

                    if (clientsConnected.Count <= 0)    //no one is connected
                        continue;

                    //build list to check readable sockets
                    //(every frame needs to be rebuilt because the list itself will be modified every frame by this method)
                    BuildReadableClientsList();

                    //filter clients that need to be read
                    Socket.Select(readCheck, writeCheck, errorCheck, 10000);
                    //iterates through those clients and check for message
                    foreach (Socket socket in readCheck)
                    {
                        var client = this.clientsConnectedDic[socket];
                        int receivedBytesAmount = 0;
                        try
                        {
                            receivedBytesAmount = tcpLayer.Receive(client.Socket, client.ReceiveBuffer);
                        }
                        catch (SocketException ex)
                        {
                            ChatLog.Info("** Client disconnected -> " + client.Name);
                            ChatLog.Info("** Reason -> " + ex.Message);

                            RemoveConnectedClient(client);

                            //notify all client that this client is gone
                            //SendMessageToAllJoined();
                            continue;
                        }

                        if (receivedBytesAmount <= 0)   //no data received
                            continue;

                        //parse packet
                        Packet receivedPacket = PacketUtilities.Read(client.ReceiveBuffer);
                        var command = (Protocol)receivedPacket.command;

                        if (!this.packetsSupportedMap.Has(command))
                            continue;

                        scheduler.Schedule(() =>
                        {
                            this.packetsSupportedMap[command](receivedPacket, client);  //invoke the callback in the scheduled thread
                        });
                    }
                }

                //send quit to all clients because the server is dead
                SendMessageToAllJoined(PacketUtilities.Build(new ProtocolObject.ServerClosed()
                {
                    message = "Server closed"
                }));
            }); //end of lambda
            receivingThread.Start();

            var commandsThread = new Thread(() =>
            {
                while (!exit)
                {
                    ChatLog.Info("** Waiting command **");
                    string command = Console.ReadLine();

                    scheduler.Schedule(() => 
                    {
                        var executed = CommandExecutor.Execute(command, scheduler);
                        if (!executed)
                            ChatLog.Warning("Command not executed: Unexisting command -> " + command);
                    });
                }
            }); //end of lambda
            commandsThread.Start();

            //main thread
            while (!exit)
            {
                scheduler.Update();
            }
        }
        /// <summary>
        /// Makes the server quit
        /// </summary>
        public void Exit()
        {
            exit = true;
        }

        #region COMMANDS_HANDLING
        void AskAllConnected(Packet receivedPacket, ChatClientData client)
        {
            var packet = PacketUtilities.GetProtocolObject<ProtocolObject.Join>(receivedPacket);
            if (!client.HasJoined)
            {
                ChatLog.Info("-- Rejected MESSAGE command by a not joined client -> " + client.Socket.RemoteEndPoint.ToString());
                return;
            }

            client.Socket.Send(PacketUtilities.Build(new ProtocolObject.GetAllConnected()
            {
                names = (from c in clientsConnected where c.Name != client.Name && c.Name != "" select c.Name).ToArray()    //empty names are not joined
            }));
        }
        void MessageSent(Packet receivedPacket, ChatClientData client)
        {
            var message = PacketUtilities.GetProtocolObject<ProtocolObject.Message>(receivedPacket);
            //TO DO IMPLEMENT A MESSAGE FOR NOT DELIVERED MESSAGES
            ChatLog.Info("To -> " + message.destinationUser + " --- Message -> " + message.message);   //print anyway

            //if not exist ignore the message
            if (!ClientExists(message.destinationUser))
            {
                ChatLog.Info("-- Message ignored because the destination user is not connected -> " + message.destinationUser);
                return;
            }
            if (!client.HasJoined)
            {
                ChatLog.Info("-- Rejected MESSAGE command by a not joined client -> " + client.Socket.RemoteEndPoint.ToString());
                return;
            }

            //send message to the other client and confirm 
            var destinationClient = GetClientByName(message.destinationUser);
            if (destinationClient != null)
            {
                destinationClient.Socket.Send(PacketUtilities.Build(new ProtocolObject.MessageReceived()
                {
                    senderUser = client.Name,
                    message = message.message //LOL
                }));
            }
        }
        void QuitReceived(Packet receivedPacket, ChatClientData client)
        {
            var quit = PacketUtilities.GetProtocolObject<ProtocolObject.Quit>(receivedPacket);

            if (!client.HasJoined)
            {
                ChatLog.Info("-- Rejected QUIT command by a not joined client -> " + client.Socket.RemoteEndPoint.ToString());
                return;
            }

            ChatLog.Info("** Client disconnected -> " + client.Name);
            ChatLog.Info("** Reason -> Wants to quit");

            var bytes = PacketUtilities.Build(new ProtocolObject.ClientLeft()
            {
                name = client.Name,
                message = "Good bye!"
            });

            //notify all client that this client is gone
            SendMessageToAllJoined(bytes);

            RemoveConnectedClient(client);
        }
        void JoinReceived(Packet receivedPacket, ChatClientData client)
        {
            var join = PacketUtilities.GetProtocolObject<ProtocolObject.Join>(receivedPacket);

            if (client.HasJoined || ClientExists(join.name))
            {
                ChatLog.Info("-- Client alredy joined. Refusing incoming request -> " + client.Name);
                var bytes = PacketUtilities.Build(new ProtocolObject.ClientJoined()
                {
                    succesful = false,
                    message = "Client alredy joined",
                    name = join.name
                });
                client.Send(bytes);
                ChatLog.Info("first byte is " + bytes[0]);
            }
            else
            {
                client.HasJoined = true;
                client.Name = join.name;

                ChatLog.Info("-- New client joined -> " + client.Name);

                var bytes = PacketUtilities.Build(new ProtocolObject.ClientJoined()
                {
                    succesful = true,
                    message = "Welcome!",
                    name = join.name
                });

                //send back to all joined clients that a new client is connected
                SendMessageToAllJoined(bytes);
            }
        }
        #endregion
    }
}
