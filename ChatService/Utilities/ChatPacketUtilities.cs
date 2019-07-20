using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

//to delete
namespace ChatService.Deprecated
{
    public static class ChatPacketUtilities
    {
        public static ChatPacketBuilder Builder { get; private set; }
        public static ChatPacketParser Parser { get; private set; }

        static ChatPacketUtilities()
        {
            Builder = new ChatPacketBuilder();
            Parser = new ChatPacketParser();
        }

        public static ChatCommands ReadCommand(byte[] bytes)
        {
            var stream = new MemoryStream(bytes);
            var reader = new BinaryReader(stream);
            stream.Seek(sizeof(ulong), SeekOrigin.Begin);   //first is packet id

            int command = reader.ReadInt32();
            //if (command < 0 || command > Enum.GetNames(typeof(ChatCommands)).Length)
            //    command = 0;

            return (ChatCommands)command;
        }
    }

    public class ChatPacketBase {

        static ulong lastId;
        public ulong packetId;

        public ChatPacketBase()
        {
            packetId = lastId++;
        }
    }

    public class ChatJoinPacket : ChatPacketBase
    {
        public const ChatCommands command = ChatCommands.JOIN;
        public string name;

        public ChatJoinPacket():base()
        {

        }
    }
    public class ChatClientJoinedPacket : ChatPacketBase
    {
        public const ChatCommands command = ChatCommands.CLIENT_JOINED;
        public bool succesful;
        public string message;
        public string name;

        public ChatClientJoinedPacket() : base()
        {

        }
    }
    public class ChatQuitPacket : ChatPacketBase
    {
        public const ChatCommands command = ChatCommands.QUIT;
        public string name;

        public ChatQuitPacket() : base()
        {

        }
    }
    public class ChatClientLeftPacket : ChatPacketBase
    {
        public const ChatCommands command = ChatCommands.CLIENT_LEFT;
        public string name;
        public string message;

        public ChatClientLeftPacket() : base()
        {

        }
    }
    public class ChatMessagePacket : ChatPacketBase
    {
        public const ChatCommands command = ChatCommands.MESSAGE_SENT;
        public string destinationUser;
        public string message;

        public ChatMessagePacket() : base()
        {

        }
    }
    public class ChatMessageReceivedPacket : ChatPacketBase
    {
        public const ChatCommands command = ChatCommands.MESSAGE_RECEIVED;
        public string senderUser;
        public string message;

        public ChatMessageReceivedPacket() : base()
        {

        }
    }

    public class ChatGetAllConnectedPacket : ChatPacketBase
    {
        public const ChatCommands command = ChatCommands.GET_ALL_CONNECTED;
        public string[] names;

        public ChatGetAllConnectedPacket() :base()
        {

        }
    }

    public class ChatAskAllConnectedPacket : ChatPacketBase
    {
        public const ChatCommands command = ChatCommands.ASK_ALL_CONNECTED;

        public ChatAskAllConnectedPacket() : base()
        {

        }
    }

    public class ChatServerClosed : ChatPacketBase
    {
        public const ChatCommands command = ChatCommands.SERVER_CLOSED;

        public string message;

        public ChatServerClosed() : base()
        {

        }
    }
}
