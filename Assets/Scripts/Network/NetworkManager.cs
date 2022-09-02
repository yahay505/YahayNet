using System.Collections.Generic;
using Network.Physics;
using Network.Player;
using UnityTools;

namespace Network
{



    public class NetworkManager : UniversalComponent<NetworkManager>
    {
        public const int fixed2NetworkRatio = 1;

        public bool isConnected =>
            selectedServerOrClient && (isServer ? ServerManager.isConnected : ClientManager.isConnected);
#if UNITY_EDITOR
        public int PeerID =>
            isServer ? 1 : 2;
        #endif
#if !UNITY_EDITOR
                public int PeerID =>
            isServer ? 1 : ClientManager.ID;
#endif


        public List<Peer> peers=new List<Peer>();
        public Peer LocalPeer;

        public bool isServer;
        private bool selectedServerOrClient;
        private int connTick;
        private void FixedUpdate()
        {
            if (isConnected)
                connTick++;
            
            if (selectedServerOrClient)
            {
                if (isServer)
                {

                    serverUpdate();
                }
                else
                {
                    clientUpdate();
                }
            }

        }

        public void SetupServer()
        {
            
            ServerManager.StartServer();
            NetworkPhysicsManager.main.InitializeAsServer();
            isServer = true;
            selectedServerOrClient = true;
            LocalPeer = new Peer(1);
            peers.Add(LocalPeer);
            AvatarManager.main.SummonAvatar(LocalPeer);
        }
        private void serverUpdate()
        {
            var handlePhysics =isConnected&& connTick % fixed2NetworkRatio == 0;
            ServerManager.ServerUpdate(handlePhysics);
            if (handlePhysics)
            {
                NetworkPhysicsManager.main.ServerNetworkPhysicsTick();
            }
        }


        public void SetupClient()
        {
            ClientManager.Connect();
            NetworkPhysicsManager.main.InitializeAsClient();

            foreach (var handler in Packets.Server2Client.Handlers)
            {
                ClientManager.RegisterPacketHandler(handler.Key,handler.Value);
                
            }
            
            
            isServer = false;
            selectedServerOrClient = true;
            LocalPeer = new Peer(2);
            peers.Add(LocalPeer);
            AvatarManager.main.SummonAvatar(LocalPeer);
        }
 
        private void clientUpdate()
        {
            var handlePhysics = connTick % fixed2NetworkRatio == 0;

            ClientManager.ClientUpdate(handlePhysics);
            if (handlePhysics)
            {
                NetworkPhysicsManager.main.ClientNetworkPhysicsTick();
            }
        }

        public void NewPeer(int peerID)
        {
            if (peerID==LocalPeer.ID)
            {
                return;
            }
            AvatarManager.main.SummonAvatar(new Peer(peerID));
            peers.Add(new Peer(peerID));
        }
    }

 
}

