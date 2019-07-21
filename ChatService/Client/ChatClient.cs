using System;
using ChatService.Log;
using ChatService.Packets;
using System.Collections.Generic;
using ChatService.Room;
/*
 
    how to implement in unity
    1 behaviour should update the scheduler
    1 behaviour should handle input
    the thread to receive is handles by the chatclient class

 */
namespace ChatService.Client
{
    /// <summary>
    /// All the actions are executed by the scheduler
    /// Remember that update the scheduler is your responsability. The dll can't know where you want to execute your code
    /// You can simply handle that calling UpdateScheduler() 
    /// </summary>
    public class ChatClient : ChatNetworkObject
    {
        public bool IsJoined { get; private set; }

        private string clientName;
        private bool logEnabled;
        private bool shouldStopListening;

        private PacketMap<Packet, object> packetsSupportedMap;

        public Dictionary<string, ChatRoom> JoinedRooms { get; private set; }   //name, obj

        /// <summary>
        /// Called whe a client join the system 
        /// </summary>
        public Action<ProtocolObject.ClientJoined> onClientJoined;
        /// <summary>
        /// Called immediately before the dispose of the socket 
        /// </summary>
        public Action<ProtocolObject.ClientLeft> onClientLeft;
        /// <summary>
        /// Called when a valid message is received  
        /// </summary>
        public Action<ProtocolObject.MessageReceived> onMessageReceived;
        /// <summary>
        /// Called when a list of connected users is received 
        /// </summary>
        public Action<ProtocolObject.GetAllConnected> onGetConnectedUsersReceived;
        /// <summary>
        /// Called when the server shut down for some reason 
        /// </summary>
        public Action<ProtocolObject.ServerClosed> onServerClosed;

        public ChatClient(string server, int port, string clientName, bool logEnabled = false)
        {
            scheduler = new TaskScheduler();
            tcpLayer = new ChatTcpLayer(true, this.scheduler, PacketUtilities.PACKET_SIZE);

            tcpLayer.SetServer(server, port);

            this.clientName = clientName;
            this.logEnabled = logEnabled;

            CommandExecutor = new CommandExecutor();
            JoinedRooms = new Dictionary<string, ChatRoom>();

            packetsSupportedMap = new PacketMap<Packet, object>();
            packetsSupportedMap[Protocol.CLIENT_JOINED] = ReceiveClientJoined;
            packetsSupportedMap[Protocol.CLIENT_LEFT] = ReceiveClientLeft;
            packetsSupportedMap[Protocol.MESSAGE_RECEIVED] = ReceiveMessageReceived;
            packetsSupportedMap[Protocol.GET_ALL_CONNECTED] = ReceiveGetAllConnected;
            packetsSupportedMap[Protocol.SERVER_CLOSED] = ReceiveServerClosed;

            packetsSupportedMap[Protocol.ROOM_CREATED] = ReceiveRoomCreated;
            packetsSupportedMap[Protocol.ROOM_CLOSED] = ReceiveRoomClosed;
            packetsSupportedMap[Protocol.ROOM_JOINED] = ReceiveRoomJoined;
            packetsSupportedMap[Protocol.ROOM_LEFT] = ReceiveRoomLeft;
        }

        public void UpdateScheduler()
        {
            this.scheduler.Update();
        }

        /// <summary>
        /// This operation is allowed only for clients not joined. Return false if operations is not allowed
        /// </summary>
        /// <returns></returns>
        public bool ChangeClientName(string newName)
        {
            if (this.IsJoined)
                return false;

            this.clientName = newName;
            return true;
        }

        /// <summary>
        /// This doesn't make sense yet.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="clientName"></param>
        /// <param name="logEnabled"></param>
        void Restart(string server, int port, string clientName, bool logEnabled = false)
        {
            try
            {
                tcpLayer.Disconnect(false);
            }
            catch
            { }

            try
            {
                tcpLayer.Close();
            }
            catch
            { }

            tcpLayer.SetServer(server, port);

            //new tcp layer?
            //tcp layer method to reinitialize the socket

            this.clientName = clientName;
            this.logEnabled = logEnabled;
        }

        #region CLIENT_RELATED
        /// <summary>
        /// Start a connection with the server
        /// </summary>
        /// <param name="autoJoin">If true automatically send a join message if connection has gone positively</param>
        /// <returns></returns>
        public bool Connect(bool autoJoin = true)
        {
            try
            {
                tcpLayer.Connect();
                //start listening
                StartListenAsync(PacketUtilities.PACKET_SIZE);
            }
            catch (Exception ex)
            {
                //no server available
                ChatLog.Info("Failed to connect -> " + ex.Message);
                return false;
            }

            //join
            if (autoJoin)
                Join();
            return true;
        }
        /// <summary>
        /// Starts new thread dedicated to listen for incoming packets (not new connections)
        /// </summary>
        /// <param name="bufferSize"></param>
        public void StartListenAsync(int bufferSize)
        {
            var t = new System.Threading.Thread(() =>
            {
                while (!shouldStopListening)
                {
                    Listen();
                }
                shouldStopListening = false;
            });
            t.Start();
        }
        /// <summary>
        /// This method handle the received messages
        /// </summary>
        void Listen()
        {
            try
            {
                int rec = tcpLayer.SimpleReceive();
                if (rec <= 0)
                    return;

                //parse packet
                Packet receivedPacket = PacketUtilities.Read(tcpLayer.ReceiveBuffer);
                var command = (Protocol)receivedPacket.command;

                if (!this.packetsSupportedMap.Has(command))
                    return;

                scheduler.Schedule(() =>
                {
                    this.packetsSupportedMap[command](receivedPacket, null);  //invoke the callback in the scheduled thread
                });
            }
            catch (Exception ex)
            {
                ChatLog.Info(ex.Message);
            }
        }

        #endregion

        #region MESSAGES_TO_RECEIVE
        void ReceiveClientJoined(Packet packet, object arg)
        {
            var p = PacketUtilities.GetProtocolObject<ProtocolObject.ClientJoined>(packet);

            if (p.succesful && p.name == this.clientName)
            {
                this.IsJoined = true;
                if (logEnabled)
                    ChatLog.Info("YOU'RE JOINED -> " + p.name + " -- " + p.succesful);
            }
            else if (p.succesful)
            {
                //another client joined
                if (logEnabled)
                    ChatLog.Info("ANOTHER CLIENT_JOINED -> " + p.name + " -- " + p.succesful);
            }

            if (onClientJoined != null)
                scheduler.Schedule(() => onClientJoined.Invoke(p));
        }
        void ReceiveClientLeft(Packet packet, object arg)
        {
            var p = PacketUtilities.GetProtocolObject<ProtocolObject.ClientLeft>(packet);

            if (logEnabled)
            {
                ChatLog.Info(p.name + " LEFT THE SERVICE -> " + p.message);
            }

            if (p.name == this.clientName)
            {
                this.IsJoined = false;
                shouldStopListening = true; //this dont close the loop in the thread, check it properly
            }

            if (onClientLeft != null)
                scheduler.Schedule(() => onClientLeft.Invoke(p));

            if (p.name == this.clientName)  //it's me
            {
                tcpLayer.Disconnect(false);
                tcpLayer.Close();
            }
        }
        void ReceiveMessageReceived(Packet packet, object arg)
        {
            var p = PacketUtilities.GetProtocolObject<ProtocolObject.MessageReceived>(packet);

            if (logEnabled)
            {
                if (string.IsNullOrEmpty(p.roomName))
                {
                    //not a room
                    ChatLog.Info("Message from -> " + p.senderUser);
                    ChatLog.Info("Message -> " + p.message);
                }
                else
                {
                    ChatLog.Info("ROOM -> " + p.roomName);
                    ChatLog.Info("      Sender -> " + p.senderUser);
                    ChatLog.Info("      Message -> " + p.message);
                }
                
            }

            if (onMessageReceived != null)
                scheduler.Schedule(() => onMessageReceived.Invoke(p));
        }
        void ReceiveGetAllConnected(Packet packet, object arg)
        {
            var p = PacketUtilities.GetProtocolObject<ProtocolObject.GetAllConnected>(packet);

            if (logEnabled)
            {
                ChatLog.Info("Clients connected: ");
                foreach (var name in p.names)
                {
                    ChatLog.Info("--** -> " + name);
                }
            }

            if (onGetConnectedUsersReceived != null)
                scheduler.Schedule(() => onGetConnectedUsersReceived.Invoke(p));
        }
        void ReceiveServerClosed(Packet packet, object arg)
        {
            var p = PacketUtilities.GetProtocolObject<ProtocolObject.ServerClosed>(packet);

            if (logEnabled)
            {
                ChatLog.Info("Server disconnected -> " + p.message);
            }

            if (onServerClosed != null)
                scheduler.Schedule(() => onServerClosed.Invoke(p));
        }
        void ReceiveRoomCreated(Packet packet, object arg)
        {
            var p = PacketUtilities.GetProtocolObject<ProtocolObject.RoomCreated>(packet);

            if (logEnabled)
            {
                ChatLog.Info("Room created success? -> " + p.success + " --- Room name -> " + p.roomName + " --- Room host -> " + p.roomHost);
                ChatLog.Info("message -> " + p.message);
            }

            if (p.success)
                JoinedRooms.Add(p.roomName, new ChatRoom(false, p.roomName, p.roomHost));
            //todo implement callback
        }
        void ReceiveRoomClosed(Packet packet, object arg)
        {
            var p = PacketUtilities.GetProtocolObject<ProtocolObject.RoomClosed>(packet);

            if (logEnabled)
            {
                ChatLog.Info("Room closed success? -> " + p.success + " --- Room name -> " + p.roomName);
                ChatLog.Info("message -> " + p.message);

                if (this.clientName == p.roomHost)
                    ChatLog.Info("I'm the host, should act differently");
            }

            //todo implement callback

            if (JoinedRooms.ContainsKey(p.roomName))
                JoinedRooms.Remove(p.roomName);
        }
        void ReceiveRoomJoined(Packet packet, object arg)
        {
            var p = PacketUtilities.GetProtocolObject<ProtocolObject.RoomJoined>(packet);

            if (logEnabled)
            {
                ChatLog.Info("Room joined success? -> " + p.success + " --- Room name -> " + p.roomName);
                ChatLog.Info("message -> " + p.message);

                if (this.clientName == p.sender)
                    ChatLog.Info("I'm the host, should show new client");
            }

            //todo implement callback

            if (p.sender == this.clientName) //it's me
            {
                if (p.success)
                {
                    ChatLog.Info("ADD CLIENT JOINED -> " + p.newUserName);
                    JoinedRooms[p.roomName].Members.Add(p.newUserName);
                }
                else
                {
                    ChatLog.Info("CLIENT NOT JOINED -> " + p.newUserName);
                }
            }
            else
            {
                //new user joined
                if (p.success)
                    JoinedRooms.Add(p.roomName, new ChatRoom(false, p.roomName, p.sender));
                //not possible not success (message will be not sent)
            }

        }
        void ReceiveRoomLeft(Packet packet, object arg)
        {
            var p = PacketUtilities.GetProtocolObject<ProtocolObject.RoomLeft>(packet);

            if (logEnabled)
            {
                ChatLog.Info("Room left success? -> " + p.success + " --- Room name -> " + p.roomName);
            }

            //todo implement callback

            if (p.userName == this.clientName) //it's me
            {
                if (p.success)
                {
                    ChatLog.Info("ROOM LEFT -> " + p.roomName);
                    JoinedRooms.Remove(p.roomName);
                    //callback for me
                }
            }
            else
            {
                //new user joined
                if (p.success)
                    JoinedRooms[p.roomName].Members.Remove(p.userName);
            }
        }
        #endregion

        #region MESSAGES_TO_SEND
        /// <summary>
        /// Sends JOIN message through the tcp layer
        /// </summary>
        public void Join()
        {
            if (!tcpLayer.Connected)
            {
                ChatLog.Info("CLIENT NOT CONNECTED");
                return;
            }

            var bytes = PacketUtilities.Build(new ProtocolObject.Join()
            {
                name = clientName
            });
            tcpLayer.Send(bytes);
        }
        /// <summary>
        /// Sends QUIT message through the tcp layer
        /// </summary>
        public void Quit()
        {
            if (!tcpLayer.Connected)
            {
                ChatLog.Info("CLIENT NOT CONNECTED");
                return;
            }

            var bytes = PacketUtilities.Build(new ProtocolObject.Quit()
            {
                name = clientName
            });
            tcpLayer.Send(bytes);
        }
        /// <summary>
        /// Sends MESSAGE message through the tcp layer
        /// </summary>
        /// <param name="to">The user which is the destination of the message. You can retrieve the list of connected users with ask_ message</param>
        /// <param name="message">The texts will be sent</param>
        public void SendMessage(string to, string message, bool isRoom)
        {
            if (!tcpLayer.Connected)
            {
                ChatLog.Info("CLIENT NOT CONNECTED");
                return;
            }

            var bytes = PacketUtilities.Build(new ProtocolObject.Message()
            {
                isRoom = isRoom,
                destinationUser = to,
                message = message
            });
            tcpLayer.Send(bytes);
        }
        /// <summary>
        /// Sends ASK message through the tcp layer
        /// </summary>
        public void AskForConnectedUsers()
        {
            if (!tcpLayer.Connected)
            {
                ChatLog.Info("CLIENT NOT CONNECTED");
                return;
            }

            var bytes = PacketUtilities.Build(new ProtocolObject.AskAllConnected() { });
            tcpLayer.Send(bytes);
        }
        /// <summary>
        /// Send the room creation message to the server (you'll be the only user joined and the host)
        /// </summary>
        /// <param name="roomName"></param>
        public void CreateRoom(string roomName)
        {
            if (!tcpLayer.Connected)
            {
                ChatLog.Info("CLIENT NOT CONNECTED");
                return;
            }

            var bytes = PacketUtilities.Build(new ProtocolObject.CreateRoom()
            {
                roomHost = this.clientName,
                roomName = roomName
            });
            tcpLayer.Send(bytes);
        }
        /// <summary>
        /// Send the close room request, if success the room will be closed and all clients kicked out (can 
        /// </summary>
        /// <param name="roomName"></param>
        public void CloseRoom(string roomName)
        {
            if (!tcpLayer.Connected)
            {
                ChatLog.Info("CLIENT NOT CONNECTED");
                return;
            }

            var bytes = PacketUtilities.Build(new ProtocolObject.CloseRoom()
            {
                roomName = roomName,
            });
            tcpLayer.Send(bytes);
        }
        /// <summary>
        /// Send an invite message to antoher specific client
        /// </summary>
        /// <param name="roomName"></param>
        /// <param name="newUser"></param>
        public void Invite(string roomName, string newUser)
        {
            if (!tcpLayer.Connected)
            {
                ChatLog.Info("CLIENT NOT CONNECTED");
                return;
            }

            if (JoinedRooms.ContainsKey(roomName) && JoinedRooms[roomName].Members.Contains(newUser))
            {
                ChatLog.Info("CLIENT ALREDY JOINED");
                return;
            }

            var bytes = PacketUtilities.Build(new ProtocolObject.InviteClient()
            {
                roomName = roomName,
                newUserName = newUser
            });
            tcpLayer.Send(bytes);
            ChatLog.Info("JOIN INVITE SENT TO " + newUser + " FOR ROOM " + roomName);
        }
        /// <summary>
        /// Send the request to leave the given room
        /// </summary>
        /// <param name="roomName"></param>
        public void LeaveRoom(string roomName)
        {
            if (!tcpLayer.Connected)
            {
                ChatLog.Info("CLIENT NOT CONNECTED");
                return;
            }

            if (!JoinedRooms.ContainsKey(roomName))
            {
                ChatLog.Info("ROOM NOT JOINED");
                return;
            }

            var bytes = PacketUtilities.Build(new ProtocolObject.LeaveRoom()
            {
                roomName = roomName
            });
            tcpLayer.Send(bytes);
            ChatLog.Info("LEAVE REQUEST SENT FOR ROOM " + roomName);
        }
        #endregion
    }
}
