using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//todelete
namespace ChatService.Deprecated
{
    public enum ChatCommands
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
}
