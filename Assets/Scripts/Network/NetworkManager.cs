using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Network.Physics;
using Unity.VisualScripting;
using UnityEngine;
using UnityTools;

namespace Network
{



    public class newNetworkManager : UniversalComponent<newNetworkManager>
    {
        public const int fixed2NetworkRatio = 1;

        public bool isConnected =>
            selectedServerOrClient && (isServer ? ServerManager.isConnected : ClientManager.isConnected);
        private bool isServer;
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
            
            isServer = false;
            selectedServerOrClient = true;
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
    }

 
}

