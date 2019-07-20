using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatService.Packets
{
    /// <summary>
    /// This class is supposed to help in managing incoming packets and responses via a prebuilt map, avoiding ugly switches
    /// </summary>
    public class PacketMap<T, K> 
    {
        private Dictionary<Protocol, Action<T,K>> supportedMessages;

        public PacketMap()
        {
            supportedMessages = new Dictionary<Protocol, Action<T, K>>();
        }
        
        public bool Has(Protocol key)
        {
            return supportedMessages.ContainsKey(key);
        }

        public Action<T, K> this[Protocol key]    // Indexer declaration  
        {
            get { return supportedMessages[key]; }
            set { supportedMessages[key] = value; }
        }
    }
}
