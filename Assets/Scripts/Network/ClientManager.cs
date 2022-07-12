using System;
using Network.Transport.TCP;
using UnityEngine;

namespace Network
{
    public static class ClientManager
    {
        private static ITransportRemoteServer server;
        private static ITransportClient client;
        public static bool isConnected => client.connected;
        public static int ID => client.ID;
        public static void Connect()
        {
            if(tryConnectViaUdp())
            {
                return;
            }

            if(tryConnectViaTCP())
            {
                return;
            }
        }

        private static bool tryConnectViaTCP()
        {
            try
            {
                var _client = new TCPClient();
                _client.Initialize();
                if (!_client.TryConnect())
                    throw new Exception("Could not Connect to Server");
                client = _client;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }

            
        }

        private static bool tryConnectViaUdp()
        {
            return false;
        }

        private static int frameNo;
        public static void ClientUpdate(bool flushNetworkFrame)
        {
            client.TransportHouseKeeping();
            client.HandlePackets();
            if (flushNetworkFrame)
            {
                client.NewNetworkFrame(++frameNo);
            }
        }

        public static void RegisterPacketHandler(int id, Action<Packet> _handler) =>
            client.RegisterPacketHandler(id, _handler);

        public static void Send(Packet _packet, TransportMode transportMode) => client.Send(_packet, transportMode);
    }
}