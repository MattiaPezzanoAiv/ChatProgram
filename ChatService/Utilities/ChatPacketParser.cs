using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//to delete
namespace ChatService.Deprecated
{
    /// <summary>
    /// This class helps in parse protocol based buffers
    /// </summary>
    public class ChatPacketParser
    {
        public ChatJoinPacket Join(byte[] bytes)
        {
            MemoryStream stream = new MemoryStream(bytes);
            BinaryReader reader = new BinaryReader(stream);

            ChatJoinPacket join = new ChatJoinPacket();
            stream.Seek(0, SeekOrigin.Begin);

            join.packetId = reader.ReadUInt64();
            reader.ReadInt32(); //command
            join.name = reader.ReadString();

            return join;
        }

        public ChatClientJoinedPacket ClientJoined(byte[] bytes)
        {
            MemoryStream stream = new MemoryStream(bytes);
            BinaryReader reader = new BinaryReader(stream);

            ChatClientJoinedPacket clientJoined = new ChatClientJoinedPacket();
            stream.Seek(0, SeekOrigin.Begin);

            clientJoined.packetId = reader.ReadUInt64();
            reader.ReadInt32(); //command
            clientJoined.succesful = reader.ReadBoolean();
            clientJoined.message = reader.ReadString();
            clientJoined.name = reader.ReadString();

            return clientJoined;
        }

        public ChatQuitPacket Quit(byte[] bytes)
        {
            MemoryStream stream = new MemoryStream(bytes);
            BinaryReader reader = new BinaryReader(stream);

            ChatQuitPacket quit = new ChatQuitPacket();
            stream.Seek(0, SeekOrigin.Begin);

            quit.packetId = reader.ReadUInt64();
            reader.ReadInt32(); //command
            quit.name = reader.ReadString();

            return quit;
        }
        public ChatClientLeftPacket ClientLeft(byte[] bytes)
        {
            MemoryStream stream = new MemoryStream(bytes);
            BinaryReader reader = new BinaryReader(stream);

            ChatClientLeftPacket clientLeft = new ChatClientLeftPacket();
            stream.Seek(0, SeekOrigin.Begin);

            clientLeft.packetId = reader.ReadUInt64();
            reader.ReadInt32(); //command
            clientLeft.name = reader.ReadString();
            clientLeft.message = reader.ReadString();

            return clientLeft;
        }

        public ChatMessagePacket Message(byte[] bytes)
        {
            MemoryStream stream = new MemoryStream(bytes);
            BinaryReader reader = new BinaryReader(stream);

            ChatMessagePacket message = new ChatMessagePacket();
            stream.Seek(0, SeekOrigin.Begin);

            message.packetId = reader.ReadUInt64();
            reader.ReadInt32(); //command
            message.destinationUser = reader.ReadString();
            message.message = reader.ReadString();

            return message;
        }

        public ChatMessageReceivedPacket MessageReceived(byte[] bytes)
        {
            MemoryStream stream = new MemoryStream(bytes);
            BinaryReader reader = new BinaryReader(stream);

            ChatMessageReceivedPacket message = new ChatMessageReceivedPacket();
            stream.Seek(0, SeekOrigin.Begin);

            message.packetId = reader.ReadUInt64();
            reader.ReadInt32(); //command
            message.senderUser = reader.ReadString();
            message.message = reader.ReadString();

            return message;
        }

        public ChatGetAllConnectedPacket GetAllConnected(byte[] bytes)
        {
            MemoryStream stream = new MemoryStream(bytes);
            BinaryReader reader = new BinaryReader(stream);

            ChatGetAllConnectedPacket packet = new ChatGetAllConnectedPacket();
            stream.Seek(0, SeekOrigin.Begin);

            packet.packetId = reader.ReadUInt64();
            reader.ReadInt32(); //command

            string rawString = reader.ReadString();
            string[] names = new string[0];
            if (!string.IsNullOrEmpty(rawString))
                names= rawString.Split(',');
            
            packet.names = names;

            return packet;
        }

        public ChatAskAllConnectedPacket AskAllConnected(byte[] bytes)
        {
            MemoryStream stream = new MemoryStream(bytes);
            BinaryReader reader = new BinaryReader(stream);

            ChatAskAllConnectedPacket packet = new ChatAskAllConnectedPacket();
            stream.Seek(0, SeekOrigin.Begin);

            packet.packetId = reader.ReadUInt64();
            reader.ReadInt32(); //command

            return packet;
        }

        public ChatServerClosed ServerClosed(byte[] bytes)
        {
            MemoryStream stream = new MemoryStream(bytes);
            BinaryReader reader = new BinaryReader(stream);

            ChatServerClosed packet = new ChatServerClosed();
            stream.Seek(0, SeekOrigin.Begin);

            packet.packetId = reader.ReadUInt64();
            reader.ReadInt32(); //command
            packet.message = reader.ReadString();

            return packet;
        }
    }
}
