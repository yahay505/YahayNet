using System.Linq;
using UnityTools;

namespace Network
{
    public  static partial class Packets
    {
        public static partial class Server2Client
        {
            public static Packet NewPeer(ITransportRemoteClient client)
            {
                var ID = 13;
                var pac = new Packet(ID);
                pac.Flags = PacketFlag.None;
                    pac.Write(client.ID);
                

                return pac;
            }
            public static void HandleNewPeer(Packet packet)
            {

                NetworkManager.main.NewPeer(packet.ReadInt());
            }
        }
    }
}