using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace ChatService
{
    public sealed class ChatTcpLayer
    {
        public int Port { get; private set; }
        public Socket NetSocket { get; private set; }
        public EndPoint NetEndPoint { get; private set; }

        /// <summary>
        /// This one can just schedule task, can't update
        /// </summary>
        private TaskScheduler cachedScheduler;

        public byte[] ReceiveBuffer { get; private set; }

        #region CALLBACKS
        public Action<Socket> onIncomingConnectionAccepted;
        #endregion


        /// <summary>
        /// 
        /// </summary>
        /// <param name="port">the port used by the socket</param>
        /// <param name="blocking">Should the socket block the program execution</param>
        /// <param name="scheduler">This can't be null. It will be used to execute callbacks in the thread you prefer</param>
        public ChatTcpLayer(bool blocking, TaskScheduler scheduler, int bufferSize)
        {
            this.cachedScheduler = scheduler;
            NetSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            NetSocket.Blocking = blocking;

            ReceiveBuffer = new byte[bufferSize];
        }


        #region GENERAL
        public void Disconnect(bool reuse)
        {
            NetSocket.Disconnect(reuse);
        }
        public void Close()
        {
            NetSocket.Close();
        }
        public bool Connected { get { return NetSocket.Connected; } }
        #endregion


        #region SERVER
        /// <summary>
        /// Server only call to bind the socket to a port ready for receiving data
        /// </summary>
        /// <param name="bindingAddress"></param>
        public void Bind(string bindingAddress, int port)
        {
            this.Port = port;
            NetEndPoint = new IPEndPoint(IPAddress.Parse(bindingAddress), this.Port);
            NetSocket.Bind(NetEndPoint);
        }
        /// <summary>
        /// Server only call to make the socket listen
        /// Warning: Socket must be bound before
        /// </summary>
        public void StartListen(int backLog = 10)
        {
            NetSocket.Listen(backLog);
        }
        /// <summary>
        /// This method automatically handles the exception thrown by the non blocking socket when accept is called.
        /// It uses the scheduler to execute the callbacks
        /// This just doesn't do anything if no new connections are coming
        /// Suggestion: use accept in a separate thread
        /// </summary>
        public void Accept()
        {
            try
            {
                var incomingSocket = NetSocket.Accept();
                if (onIncomingConnectionAccepted != null)
                {
                    cachedScheduler.Schedule(() => onIncomingConnectionAccepted(incomingSocket));
                }
            }
            catch (Exception ex)
            {

            }
        }
        /// <summary>
        /// Pass a valid socket that should receive data (used for servers)
        /// NB. this class does not store socket data. it's your responsability
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        public int Receive(Socket socket, byte[] buffer)
        {
            try
            {
                var receivedBytesAmount = socket.Receive(buffer);
                return receivedBytesAmount;
            }
            catch (SocketException ex)
            {
                return 0;
            }
        }
        #endregion


        #region CLIENT
        /// <summary>
        /// Just call receive from the main socket
        /// </summary>
        /// <returns>Returns the amount of bytes received. 0 if nothing is received or an exception occurs. You can access the buffer using ReceiveBuffer</returns>
        public int SimpleReceive()
        {
            try
            {
                var receivedBytesAmount = NetSocket.Receive(ReceiveBuffer);
                return receivedBytesAmount;
            }
            catch (SocketException ex)
            {
                return 0;
            }
        }
        public void SetServer(string server, int port)
        {
            this.Port = port;
            this.NetEndPoint = new IPEndPoint(IPAddress.Parse(server), this.Port);
        }
        public void Connect()
        {
            NetSocket.Connect(this.NetEndPoint);
        }
        public void Send(byte[] bytes)
        {
            NetSocket.Send(bytes);
        }
        #endregion

    }
}
