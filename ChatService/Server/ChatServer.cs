using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Net.Sockets;
using ChatService.Log;
using ChatService.Packets;
using ChatService.Room;

namespace ChatService.Server
{
    public sealed class ChatServer : ChatNetworkObject
    {
        private bool exit = false;

        private PacketMap<Packet, ChatClientData> packetsSupportedMap;
        private Dictionary<string, ChatRoom> activeRoomsByName;   //room name, room

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

            activeRoomsByName = new Dictionary<string, ChatRoom>();

            CommandExecutor = new CommandExecutor();

            packetsSupportedMap = new PacketMap<Packet, ChatClientData>();
            packetsSupportedMap[Protocol.ASK_ALL_CONNECTED] = this.AskAllConnectedReceived;
            packetsSupportedMap[Protocol.MESSAGE_SENT] = this.MessageSentReceived;
            packetsSupportedMap[Protocol.QUIT] = this.QuitReceived;
            packetsSupportedMap[Protocol.JOIN] = this.JoinReceived;

            packetsSupportedMap[Protocol.CREATE_ROOM] = this.CreateRoomReceived;
            packetsSupportedMap[Protocol.CLOSE_ROOM] = this.CloseRoomReceived;
            packetsSupportedMap[Protocol.INVITE_CLIENT] = this.InviteClientReceived;
            packetsSupportedMap[Protocol.LEAVE_ROOM] = this.LeaveRoomReceived;
        }

        #region ROOM
        public List<ChatRoom> GetActiveRooms()
        {
            return (from room in activeRoomsByName select room.Value).ToList();
        }
        public List<string> GetRoomMembers(string roomName)
        {
            return new List<string>(RoomFromName(roomName).Members);
        }
        public bool RoomExistsByName(string roomName)
        {
            return activeRoomsByName.ContainsKey(roomName);
        }
        /// <summary>
        /// Check if the given room's host is the same given host
        /// </summary>
        bool RoomHostMatch(string roomName, string roomHost)
        {
            if (activeRoomsByName.ContainsKey(roomName))
                return activeRoomsByName[roomName].Host == roomHost;
            return false;
        }
        ChatRoom RoomFromName(string roomName)
        {
            if (activeRoomsByName.ContainsKey(roomName))
                return activeRoomsByName[roomName];
            return null;
        }
        ChatRoom CreateRoom(string roomName, string roomHost, bool isPublic = false)
        {
            var room = new ChatRoom(isPublic, roomName, roomHost);
            activeRoomsByName.Add(roomName, room);
            return room;
        }
        void DeleteRoom(string roomName)
        {
            activeRoomsByName.Remove(roomName);
        }
        void ClientJoinRoom(string roomName, string clientName)
        {
            if (!RoomExistsByName(roomName))
            {
                ChatLog.Error("Room " + roomName + " does not exists");
                return;
            }

            var room = RoomFromName(roomName);
            room.Members.Add(clientName);

            //todo: send client joined
        }
        #endregion


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
        void AskAllConnectedReceived(Packet receivedPacket, ChatClientData client)
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
        void MessageSentReceived(Packet receivedPacket, ChatClientData client)
        {
            var message = PacketUtilities.GetProtocolObject<ProtocolObject.Message>(receivedPacket);
            //TO DO IMPLEMENT A MESSAGE FOR NOT DELIVERED MESSAGES
            ChatLog.Info("To -> " + message.destinationUser + " --- Message -> " + message.message);   //print anyway

            bool isToRoom = message.isRoom;

            //if not exist ignore the message
            if (!isToRoom && !ClientExists(message.destinationUser))
            {
                ChatLog.Info("-- Message ignored because the destination user is not connected -> " + message.destinationUser);
                return;
            }
            if (!isToRoom && !client.HasJoined)
            {
                ChatLog.Info("-- Rejected MESSAGE command by a not joined client -> " + client.Socket.RemoteEndPoint.ToString());
                return;
            }
            if (isToRoom && !RoomExistsByName(message.destinationUser))
            {
                ChatLog.Info("-- Rejected MESSAGE command to a room because the room does not exists -> " + message.destinationUser);
                return;
            }

            byte[] response = PacketUtilities.Build(new ProtocolObject.MessageReceived()
            {
                senderUser = client.Name,
                roomName = message.destinationUser, //empty if is not a room
                message = message.message //LOL
            });

            if (isToRoom)
            {
                ChatRoom room = RoomFromName(message.destinationUser);
                foreach (var m in room.Members)
                {
                    ChatClientData c = GetClientByName(m);
                    c.Send(response);
                    ChatLog.Info("MESSAGE SENT TO -> " + m);
                }
                var host = GetClientByName(room.Host);
                host.Send(response);
                ChatLog.Info("MESSAGE SENT TO -> " + host.Name);
            }
            else
            {
                //send message to the other client and confirm 
                var destinationClient = GetClientByName(message.destinationUser);
                if (destinationClient != null)
                {
                    destinationClient.Socket.Send(response);
                }
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

        void CreateRoomReceived(Packet receivedPacket, ChatClientData client)
        {
            var createRoom = PacketUtilities.GetProtocolObject<ProtocolObject.CreateRoom>(receivedPacket);

            bool _success = true;
            string _message = "";

            if (RoomExistsByName(createRoom.roomName))
            {
                _success = false;
                _message = "A room with the given name alredy exists.";
            }

            ChatLog.Info("*** CREATE_ROOM received --- name -> " + createRoom.roomName + " --- host -> " + createRoom.roomHost + " --- success -> " + _success);
            if (!_success)
                ChatLog.Info("      REASON -> " + _message);

            if (_success)
                CreateRoom(createRoom.roomName, createRoom.roomHost, false);   //fill dala structure

            byte[] response = PacketUtilities.Build(new ProtocolObject.RoomCreated()
            {
                roomName = createRoom.roomName,
                roomHost = createRoom.roomHost,
                success = _success,
                message = _message
            });
            client.Send(response);
        }
        void CloseRoomReceived(Packet receivedPacket, ChatClientData client)
        {
            var closeRoom = PacketUtilities.GetProtocolObject<ProtocolObject.CloseRoom>(receivedPacket);

            bool _success = true;
            string _message = "";

            //room exists
            if (!RoomExistsByName(closeRoom.roomName))
            {
                _success = false;
                _message = "A room with the given name does not exist.";
            }
            //you're the host
            else if (!RoomHostMatch(closeRoom.roomName, client.Name))
            {
                _success = false;
                _message = "You're not the host, you can't close the room";
            }

            string roomHost = "Nothing";
            if (activeRoomsByName.ContainsKey(closeRoom.roomName))
                roomHost = activeRoomsByName[closeRoom.roomName].Host;

            ChatLog.Info("CLOSE_ROOM received success? -> " + _success + " --- room name -> " + closeRoom.roomName + " --- message -> " + _message);

            byte[] response = PacketUtilities.Build(new ProtocolObject.RoomClosed()
            {
                roomName = closeRoom.roomName,
                roomHost = roomHost,  //set host then the host client can decide if he's the host or not
                success = _success,
                message = _message
            });
            client.Send(response);

            if (_success)
            {
                //if succesfully closed, notify all memebers
                var room = RoomFromName(closeRoom.roomName);
                foreach (var c in room.Members)
                {
                    ChatClientData chatClient = GetClientByName(c);
                    chatClient.Send(response);
                }
            }

            if (_success)
                DeleteRoom(closeRoom.roomName);
        }
        void InviteClientReceived(Packet receivedPacket, ChatClientData client)
        {
            var invite = PacketUtilities.GetProtocolObject<ProtocolObject.InviteClient>(receivedPacket);

            bool _success = true;
            string _message = "";

            //room exists
            if (!RoomExistsByName(invite.roomName))
            {
                _success = false;
                _message = "Room does not exists";
            }
            else if (!RoomHostMatch(invite.roomName, client.Name)) //not the host, can't invite
            {
                _success = false;
                _message = "You're not the host, you can't invite";
            }
            else if (!ClientExists(invite.newUserName))
            {
                _success = false;
                _message = "User does not exists";
            }

            ChatLog.Info("INVITE_CLIENT received success? -> " + _success + " --- room name -> " + invite.roomName + " --- message -> " + _message);

            ChatRoom room = RoomFromName(invite.roomName);

            byte[] response = PacketUtilities.Build(new ProtocolObject.RoomJoined()
            {
                success = _success,
                message = _message,
                roomName = invite.roomName,
                newUserName = invite.newUserName,
                sender = client.Name,
                members = room != null ? room.Members : new List<string>() { }
            });

            //sender response anyway
            client.Send(response);

            if (_success)
            {
                //positive notify all clients
                room.Members.Add(invite.newUserName);   //add new user

                //notify host

                foreach (var c in room.Members)
                {
                    ChatClientData chatClient = GetClientByName(c);
                    chatClient.Send(response);
                }
            }
        }
        void LeaveRoomReceived(Packet receivedPacket, ChatClientData client)
        {
            var leave = PacketUtilities.GetProtocolObject<ProtocolObject.LeaveRoom>(receivedPacket);

            bool _success = true;
            string _message = "";
            ChatRoom room = RoomFromName(leave.roomName);

            if (!RoomExistsByName(leave.roomName))
            {
                _success = false;
                _message = "Room does not exists";
            }
            //is client in the requested room
            else if (room != null && !room.Members.Contains(client.Name))
            {
                _success = false;
                _message = "Client is not int he room";
            }
            //client side, client shouldn't allow the leave if you're the host, just close room

            //build response
            byte[] response = PacketUtilities.Build(new ProtocolObject.RoomLeft()
            {
                success = _success,
                roomName = leave.roomName,
                userName = client.Name
            });

            client.Send(response);

            //is success notify all joined
            if (_success)
            {
                room.Members.Remove(client.Name);   //remove user user
                GetClientByName(room.Host).Send(response); //host is not in the members

                foreach (var m in room.Members)
                {
                    ChatClientData c = GetClientByName(m);
                    c.Send(response);
                }
            }
        }
        #endregion
    }
}
