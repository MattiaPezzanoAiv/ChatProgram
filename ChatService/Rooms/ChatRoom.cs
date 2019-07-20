using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
 
    PRIVATE ROOMS (like groups)
    rooms list object

    create_room (client -> server)
        ask to the server to create a private room

    room_created (server -> client)
        send to the client if the room is created and if it is the room data (maybe id? or just name)

    invite_user (client -> server)
        self explained (check if user exists)

    room_invite (server -> client)
        the server notify the client that it's invited to a room

    leave_room (client -> server)
        client notify to the server that it's quitting the room (say the room id) (the server should check if the client is really connected to that room)

    room_leaved (server -> client)
        the server notify ALL the clients that a specific client has left


    close_room (client -> server)
        only the host can close the room

    room_closed (server -> client)
        notify the clients that the room is closed (that makes all clients to leave the room)

 */


namespace ChatService.Room
{
    /// <summary>
    /// This class is supposed to represent a room for both client and server
    /// </summary>
    public sealed class ChatRoom
    {
        public bool IsPublic { get; private set; }

        /// <summary>
        /// Represent the host of the room (has power to destroy the room)
        /// </summary>
        public string Host { get; private set; }

        /// <summary>
        /// The name of the room
        /// </summary>
        public string Name { get; private set; }

        public List<string> Members { get; private set; }

        public ChatRoom(bool isPublic,string name, string host)
        {
            this.IsPublic = IsPublic;
            this.Host = host;
            this.Name = name;

            Members = new List<string>();
        }
    }
}
