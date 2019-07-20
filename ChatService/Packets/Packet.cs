namespace ChatService.Packets
{
    public class Packet
    {
        static ulong lastId;

        public ulong packetId;
        public int command;
        public string json;

        public Packet()
        {
            packetId = lastId++;
        }
    }
}
