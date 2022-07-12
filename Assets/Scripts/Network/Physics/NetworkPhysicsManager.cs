using System;
using System.Linq;
using UnityEngine;
using UnityTools;

namespace Network.Physics
{
    public class NetworkPhysicsManager : UniversalComponent<NetworkPhysicsManager>
    {
        #region server

        public void InitializeAsServer()
        {
            ServerManager.RegisterPacketHandler(16, ServerHandleNbodyUpdatePacket);
        }

        private void ServerHandleNbodyUpdatePacket(Packet packet, int clientID)
        {
            //todo: handle ownership
            HandleNbodyUpdatePacket(packet);
        }


        public void ServerNetworkPhysicsTick()
        {
            
            ServerManager.SendToAll(CreateServerUpdatePacket(),TransportMode.ReqNetTick);
            
            
            
            Packet CreateServerUpdatePacket()
            {
                var timeBetween = Time.fixedDeltaTime * newNetworkManager.fixed2NetworkRatio;
                const int targetKbps = 231;
                const int bpp = 54 * 8;
                var updates = FindObjectsOfType<NetworkedBody>()
                    .Where(body => (body.accumulated >= 0))
                    .OrderByDescending(body => body.accumulated)
                    .Take(
                        (int) Math.Floor(targetKbps * 1024 * timeBetween / bpp)
                        // 2
                    ).CreateManyUpdatePackets()
                    .ToArray();
                var packet = new Packet(16).With(updates.Length);
                foreach (var update in updates)
                {
                    packet.Write(update);
                }


// Debug.Log(bytes.ToHexString());
                return packet;
            }
        }

        #endregion

        #region Client
        public void InitializeAsClient()
        {
            ClientManager.RegisterPacketHandler(16, ClientHandleNbodyUpdatePacket);
        }

        private void ClientHandleNbodyUpdatePacket(Packet packet)
        {
            HandleNbodyUpdatePacket(packet);
        }
        public void ClientNetworkPhysicsTick()
        {
            
            ClientManager.Send(CreateClientUpdatePacket(),TransportMode.ReqNetTick);
            
            
            
            Packet CreateClientUpdatePacket()
            {
                var timeBetween = Time.fixedDeltaTime * newNetworkManager.fixed2NetworkRatio;
                const int targetKbps = 230;
                const int bpp = 54 * 8;
                var updates = FindObjectsOfType<NetworkedBody>()
                    .Where(body=>body.Authority==ClientManager.ID)
                    .Where(body => (body.accumulated >= 0))
                    .OrderByDescending(body => body.accumulated)
                    .Take(
                        (int) Math.Floor(targetKbps * 1024 * timeBetween / bpp)
                        // 2
                    ).CreateManyUpdatePackets()
                    .ToArray();
                var packet = new Packet(16).With(updates.Length);
                foreach (var update in updates)
                {
                    packet.Write(update);
                }


// Debug.Log(bytes.ToHexString());
                return packet;
            }
        }

        #endregion

        #region general
            private void HandleNbodyUpdatePacket(Packet packet)
            {
                var updateCount = packet.ReadInt();
                var updates= 
                   updateCount.Times(()=>packet.readNBodyUpdate());
                // var count = packet.ReadInt();
                // NBodyUpdate[] arr = new NBodyUpdate[count];
                // for (int i = 0; i < count; i++)
                // {
                //     arr[i] = packet.readNBodyUpdate();
                // }
                NetworkedBody.ApplyUpdates(updates);
            }
        #endregion
    }
}