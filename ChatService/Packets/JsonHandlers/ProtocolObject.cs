using System.Collections.Generic;

namespace ChatService.Packets
{
    public class ProtocolObject
    {
        public abstract class BaseProtocolObject
        {
            public abstract Protocol Proto { get; }
        }

        public class Join : BaseProtocolObject
        {
            public string name;

            public override Protocol Proto => Protocol.JOIN;
        }
        public class ClientJoined : BaseProtocolObject
        {
            public bool succesful;
            public string message;
            public string name;

            public override Protocol Proto => Protocol.CLIENT_JOINED;
        }
        public class Quit : BaseProtocolObject
        {
            public string name;

            public override Protocol Proto => Protocol.QUIT;
        }
        public class ClientLeft : BaseProtocolObject
        {
            public string name;
            public string message;

            public override Protocol Proto => Protocol.CLIENT_LEFT;
        }
        public class Message : BaseProtocolObject
        {
            public string destinationUser;
            public string message;

            public override Protocol Proto => Protocol.MESSAGE_SENT;
        }
        public class MessageReceived : BaseProtocolObject
        {
            public string senderUser;
            public string message;

            public override Protocol Proto => Protocol.MESSAGE_RECEIVED;
        }
        public class GetAllConnected : BaseProtocolObject
        {
            public string[] names;

            public override Protocol Proto => Protocol.GET_ALL_CONNECTED;
        }
        public class AskAllConnected : BaseProtocolObject
        {
            public override Protocol Proto => Protocol.ASK_ALL_CONNECTED;
        }
        public class ServerClosed : BaseProtocolObject
        {
            public string message;

            public override Protocol Proto => Protocol.SERVER_CLOSED;
        }


        #region ROOMS
        public class CreateRoom : BaseProtocolObject
        {
            public override Protocol Proto => Protocol.CREATE_ROOM;

            public string roomName;
            public string roomHost;
        }
        public class RoomCreated : BaseProtocolObject
        {
            public override Protocol Proto => Protocol.ROOM_CREATED;

            public string roomName;
            public string roomHost;
            public bool success;    //if false room is not created
            public string message;
        }

        public class CloseRoom : BaseProtocolObject
        {
            public override Protocol Proto => Protocol.CLOSE_ROOM;

            public string roomName;
        }
        public class RoomClosed : BaseProtocolObject
        {
            public override Protocol Proto => Protocol.ROOM_CLOSED;

            public string roomName;
            public string roomHost;
            public bool success;
            public string message;
        }

        public class LeaveRoom : BaseProtocolObject
        {
            public override Protocol Proto => Protocol.LEAVE_ROOM;

            public string roomName;
        }
        public class RoomLeft : BaseProtocolObject
        {
            public override Protocol Proto => Protocol.ROOM_LEFT;

            public string roomName;
            public string userName;
            public bool success;
        }

        public class InviteClient : BaseProtocolObject
        {
            public override Protocol Proto => Protocol.INVITE_CLIENT;

            public string newUserName;
            public string roomName;
        }
        public class RoomJoined : BaseProtocolObject
        {
            public override Protocol Proto => Protocol.ROOM_JOINED;

            public bool success;
            public string sender;
            public string newUserName;
            public string roomName;
            public List<string> members;
            public string message;
        }
        #endregion
    }
}
