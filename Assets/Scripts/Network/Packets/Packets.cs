using System;
using System.Collections.Generic;

namespace Network
{
    public  static partial class Packets
    {
        public static partial class  Server2Client
        {
            public static Dictionary<int, Action<Packet>> Handlers = new()
            {
                // {0, null},
                {13,HandleNewPeer}
            };
        }
        
        public static partial class  Client2Server
        {
            public static Dictionary<int, Action<Packet,int>> Handlers = new()
            {
                // {0, null}
            };
        }
    }
}