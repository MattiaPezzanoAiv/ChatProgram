using System.Net.Sockets;

namespace ChatService.Server
{
    /// <summary>
    /// Class used by the server to represent a client status
    /// </summary>
    public class ChatClientData
    {
        public Socket Socket{ get; set; } 
        public byte[] ReceiveBuffer { get; set; }
        public bool HasJoined { get; set; }
        public string Name { get; set; }

        /// <summary>
        /// Sends bytes through the socket associated to this client
        /// </summary>
        /// <param name="bytes"></param>
        public void Send(byte[] bytes)
        {
            Socket.Send(bytes);
        }
    }
}
