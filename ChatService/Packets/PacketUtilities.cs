using System;
using Newtonsoft.Json;
using System.IO;

namespace ChatService.Packets
{
    public enum Protocol
    {
        NONE = 0,   //avoid all operations
        JOIN = 1,       //client->server request to join the chat system
        CLIENT_JOINED = 2,   //server->client accept or refuse the incominc join request. contains the refuse reason as string
        QUIT = 3,           //client->server notify the server that the client is leaving (client decision)
        CLIENT_LEFT = 4,
        MESSAGE_SENT = 5,
        MESSAGE_RECEIVED = 6,
        ASK_ALL_CONNECTED = 7,  //client -> server
        GET_ALL_CONNECTED = 8,   //server -> client

        SERVER_CLOSED = 100
    }

    public static class PacketUtilities
    {
        public const int PACKET_SIZE = PacketUtilities.PACKET_SIZE;

        public static byte[] Build<T>(T protocolObject) where T : ProtocolObject.BaseProtocolObject
        {
            Packet packet = new Packet();
            Protocol proto = protocolObject.Proto;
            packet.command = (int)proto;
            
            packet.json = JsonConvert.SerializeObject(protocolObject);

            string json = JsonConvert.SerializeObject(packet);

            byte[] buffer = new byte[PACKET_SIZE];
            var stream = new MemoryStream(buffer);
            var writer = new BinaryWriter(stream);

            writer.Write(json);

            return buffer;
        }
        public static Packet Read(byte[] buffer)
        {
            var stream = new MemoryStream(buffer);
            var reader = new BinaryReader(stream);

            string json = reader.ReadString();
            Packet p = JsonConvert.DeserializeObject<Packet>(json);

            return p;
        }

        /// <summary>
        /// This method returns the protocol object extracted from the packet. Null if it's not consistent
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="packet"></param>
        /// <returns></returns>
        public static T GetProtocolObject<T>(Packet packet)
        {
            T protocolObject = JsonConvert.DeserializeObject<T>(packet.json);
            return protocolObject;
        }
    }
}
