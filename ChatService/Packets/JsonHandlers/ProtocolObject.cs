﻿namespace ChatService.Packets
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
    }
}
