using System;
using System.Collections.Generic;
using System.Linq;
using Network.Player;
using Network.Transport.TCP;
using UnityEngine;
using UnityTools;
using Object = UnityEngine.Object;

namespace Network
{
    public static class ServerManager
    {
        public static List<ITransportServer> Servers = new();
        public static Dictionary<int, ITransportRemoteClient> Clients=new ();

        public static bool isConnected => Clients.Count > 0;

        public static void  SendToAll(Packet packet, TransportMode transportMode)
        {
            Servers.ForEach(s=>s.SendToAll(packet,transportMode));
        }
        public static void StartServer()
        {
            var tcpServer = new TCPServer();
            Servers.Add(tcpServer);
            tcpServer.Initialize();
            Servers.ForEach(server =>
            {
                // server.RegisterPacketHandler(16, (_,_)=>{});//NbodyPacket id
                server.OnDisconnection += (id, client) =>
                {
                    Clients.Remove(id);
                    Debug.Log($"Client {id} disconnected");
                };
            });
            foreach (var handler in Packets.Client2Server.Handlers)
            {
                RegisterPacketHandler(handler.Key,handler.Value);
            }
            

            

        }

        private static int nextID = 2;
        static int GetNextID() => nextID++;
        public static void ServerUpdate(bool handleBuffered)
        {
            Servers.ForEach(server=>server.TransportHouseKeeping());
            foreach (var remoteClient in Servers.SelectMany(server=>server.AcceptIncomingConnection(GetNextID)))
            {
                Clients.Add(remoteClient.ID,remoteClient);
                
// AvatarManager.main.SummonAvatar(remoteClient);           
                SendToAll(Packets.Server2Client.NewPeer(remoteClient),TransportMode.ReqAck);
                foreach (var peer in NetworkManager.main.peers)
                {
                    remoteClient.Send(Packets.Server2Client.NewPeer(peer),TransportMode.ReqAck);
                }
                NetworkManager.main.NewPeer(remoteClient.ID);
            }

            if (handleBuffered)
            {
                Servers.ForEach(server=>server.PopBufferedPackets());
            }
            Servers.ForEach(server=>server.HandlePackets());

        }


        public static void RegisterPacketHandler(int id, Action<Packet, int> handler)
        {
            Servers.ForEach(s=>s.RegisterPacketHandler(id,handler));
        }

        public static void DeRegisterPacketHandler(int id)
        {
            Servers.ForEach(s=>s.DeRegisterPacketHandler(id));
            
        }

    }
}