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
    /// This class helps is create protocol base packets
    /// </summary>
    public class ChatPacketBuilder
    {
        public byte[] Join(ChatJoinPacket join)
        {
            byte[] bytes = new byte[PacketUtilities.PACKET_SIZE];
            MemoryStream stream = new MemoryStream(bytes);
            BinaryWriter writer = new BinaryWriter(stream);
            stream.Seek(0, SeekOrigin.Begin);

            writer.Write(join.packetId);
            writer.Write((int)ChatJoinPacket.command);
            writer.Write(join.name);

            return bytes;
        }

        public byte[] ClientJoined(ChatClientJoinedPacket clientJoined)
        {
            byte[] bytes = new byte[PacketUtilities.PACKET_SIZE];
            MemoryStream stream = new MemoryStream(bytes);
            BinaryWriter writer = new BinaryWriter(stream);
            stream.Seek(0, SeekOrigin.Begin);

            writer.Write(clientJoined.packetId);
            writer.Write((int)ChatClientJoinedPacket.command);
            writer.Write(clientJoined.succesful);
            writer.Write(clientJoined.message);
            writer.Write(clientJoined.name);

            return bytes;
        }

        public byte[] Quit(ChatQuitPacket quit)
        {
            byte[] bytes = new byte[PacketUtilities.PACKET_SIZE];
            MemoryStream stream = new MemoryStream(bytes);
            BinaryWriter writer = new BinaryWriter(stream);
            stream.Seek(0, SeekOrigin.Begin);

            writer.Write(quit.packetId);
            writer.Write((int)ChatQuitPacket.command);
            writer.Write(quit.name);

            return bytes;
        }

        public byte[] ClientLeft(ChatClientLeftPacket clientLeft)
        {
            byte[] bytes = new byte[PacketUtilities.PACKET_SIZE];
            MemoryStream stream = new MemoryStream(bytes);
            BinaryWriter writer = new BinaryWriter(stream);
            stream.Seek(0, SeekOrigin.Begin);

            writer.Write(clientLeft.packetId);
            writer.Write((int)ChatClientLeftPacket.command);
            writer.Write(clientLeft.name);
            writer.Write(clientLeft.message);

            return bytes;
        }

        public byte[] Message(ChatMessagePacket message)
        {
            byte[] bytes = new byte[PacketUtilities.PACKET_SIZE];
            MemoryStream stream = new MemoryStream(bytes);
            BinaryWriter writer = new BinaryWriter(stream);
            stream.Seek(0, SeekOrigin.Begin);

            writer.Write(message.packetId);
            writer.Write((int)ChatMessagePacket.command);
            writer.Write(message.destinationUser);
            writer.Write(message.message);

            return bytes;
        }
        public byte[] MessageReceived(ChatMessageReceivedPacket message)
        {
            byte[] bytes = new byte[PacketUtilities.PACKET_SIZE];
            MemoryStream stream = new MemoryStream(bytes);
            BinaryWriter writer = new BinaryWriter(stream);
            stream.Seek(0, SeekOrigin.Begin);

            writer.Write(message.packetId);
            writer.Write((int)ChatMessageReceivedPacket.command);
            writer.Write(message.senderUser);
            writer.Write(message.message);

            return bytes;
        }

        public byte[] GetAllConnected(ChatGetAllConnectedPacket packet)
        {
            byte[] bytes = new byte[PacketUtilities.PACKET_SIZE];
            MemoryStream stream = new MemoryStream(bytes);
            BinaryWriter writer = new BinaryWriter(stream);
            stream.Seek(0, SeekOrigin.Begin);

            writer.Write(packet.packetId);
            writer.Write((int)ChatGetAllConnectedPacket.command);

            string toWrite = "";
            for (int i = 0; i < packet.names.Length; i++)
            {
                if (i != 0)
                    toWrite += "," + packet.names[i];
                else
                    toWrite += packet.names[i];
            }
            writer.Write(toWrite);

            return bytes;
        }

        public byte[] AskAllConnected(ChatAskAllConnectedPacket packet)
        {
            byte[] bytes = new byte[PacketUtilities.PACKET_SIZE];
            MemoryStream stream = new MemoryStream(bytes);
            BinaryWriter writer = new BinaryWriter(stream);
            stream.Seek(0, SeekOrigin.Begin);

            writer.Write(packet.packetId);
            writer.Write((int)ChatAskAllConnectedPacket.command);

            return bytes;
        }

        public byte[] ServerClosed(ChatServerClosed serverClosed)
        {
            byte[] bytes = new byte[PacketUtilities.PACKET_SIZE];
            MemoryStream stream = new MemoryStream(bytes);
            BinaryWriter writer = new BinaryWriter(stream);
            stream.Seek(0, SeekOrigin.Begin);

            writer.Write(serverClosed.packetId);
            writer.Write((int)ChatAskAllConnectedPacket.command);
            writer.Write(serverClosed.message);

            return bytes;
        }
    }
}
